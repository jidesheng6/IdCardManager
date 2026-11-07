using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using IdCardManager.Application.Services;
using IdCardManager.Domain.Entities;

namespace IdCardManager.Infrastructure.Import
{
    public sealed class LrCenterParamImporter : IParamImporter
    {
        public ParamImportResult ImportFromLrCenter(string lrCenterRoot, AppSettings current)
        {
            var settings = Clone(current ?? new AppSettings());
            if (string.IsNullOrWhiteSpace(lrCenterRoot) || !Directory.Exists(lrCenterRoot))
            {
                return new ParamImportResult
                {
                    Settings = settings,
                    Partial = true,
                    Message = "原系统目录无效"
                };
            }

            settings.LrCenterPath = lrCenterRoot;
            var server = "127.0.0.1";
            var dbName = "DatCent_Single";
            var paramPath = Path.Combine(lrCenterRoot, "paraminfo.set");
            if (File.Exists(paramPath))
            {
                ParseParamInfo(File.ReadAllText(paramPath, Encoding.Default), ref server, ref dbName);
            }

            var notes = new StringBuilder();
            // 本机常见实例名依次尝试
            var servers = new[] { "(local)", "127.0.0.1", ".", "localhost", server };
            var ok = false;
            foreach (var candidate in servers)
            {
                if (string.IsNullOrWhiteSpace(candidate)) continue;
                if (TryImportFromSql(settings, candidate, dbName, notes))
                {
                    ok = true;
                    break;
                }
            }

            if (!ok)
            {
                return new ParamImportResult
                {
                    Settings = settings,
                    Partial = true,
                    Message = "未能读取系统密钥请确认本机 SQL Server 已启动且库为 " + dbName
                };
            }

            var partial = string.IsNullOrWhiteSpace(settings.UserKey);
            return new ParamImportResult
            {
                Settings = settings,
                Partial = partial,
                Message = partial
                    ? "已连接数据库但未读到 AppKey"
                    : "已从原系统库导入密钥与站点"
            };
        }

        private static void ParseParamInfo(string text, ref string server, ref string dbName)
        {
            foreach (var rawLine in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.StartsWith("[") || !line.Contains("="))
                {
                    continue;
                }

                var idx = line.IndexOf('=');
                var key = line.Substring(0, idx).Trim();
                var value = line.Substring(idx + 1).Trim();
                if (string.Equals(key, "servername", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value))
                {
                    server = value;
                }
                else if (string.Equals(key, "dbname", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value))
                {
                    dbName = value;
                }
            }
        }

        private static bool TryImportFromSql(AppSettings settings, string server, string dbName, StringBuilder notes)
        {
            try
            {
                var cs = "Data Source=" + server + ";Initial Catalog=" + dbName + ";Integrated Security=True;Connect Timeout=3";
                using (var conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT TOP 1 RTRIM(AppKey) FROM SYSCONF WHERE AppKey IS NOT NULL AND RTRIM(AppKey) <> ''";
                        var key = cmd.ExecuteScalar() as string;
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            settings.UserKey = key.Trim();
                        }
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT TOP 1 RTRIM(StationNo), RTRIM(Area) FROM StationInfo ORDER BY StationNo";
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    settings.StationNo = reader.GetString(0).Trim();
                                    if (string.IsNullOrWhiteSpace(settings.TerminalNo) || settings.TerminalNo == "00000001")
                                    {
                                        settings.TerminalNo = settings.StationNo;
                                    }
                                }

                                if (!reader.IsDBNull(1))
                                {
                                    settings.AreaNo = reader.GetString(1).Trim();
                                }
                            }
                        }
                    }
                }

                return !string.IsNullOrWhiteSpace(settings.UserKey);
            }
            catch (Exception ex)
            {
                notes.Append(ex.Message);
                return false;
            }
        }

        private static AppSettings Clone(AppSettings s)
        {
            return new AppSettings
            {
                TerminalNo = s.TerminalNo,
                UserKey = s.UserKey,
                StationNo = s.StationNo,
                AreaNo = s.AreaNo,
                OperatorName = s.OperatorName,
                LrCenterPath = s.LrCenterPath,
                UserCardNad = s.UserCardNad,
                AuthCardNad = s.AuthCardNad,
                ReaderType = s.ReaderType,
                ReaderPort = s.ReaderPort,
                ReaderPollMs = s.ReaderPollMs,
                UseFakeCard = s.UseFakeCard
            };
        }
    }
}

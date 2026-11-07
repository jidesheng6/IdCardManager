using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace IdCardManager.Infrastructure.Card
{
    public static class OilTypeCatalog
    {
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "1020", "92号汽油" },
            { "1040", "95号汽油" },
            { "1030", "93号汽油" },
            { "1050", "97号汽油" },
            { "1090", "98号汽油" }
        };

        private static bool _loaded;

        public static void TryLoadFromSql(string server, string database)
        {
            if (_loaded) return;
            try
            {
                var cs = "Data Source=" + server + ";Initial Catalog=" + database + ";Integrated Security=True;Connect Timeout=2";
                using (var conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT RTRIM(OilCode), RTRIM(OilName) FROM OilInfo";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var code = reader.IsDBNull(0) ? null : reader.GetString(0);
                                var name = reader.IsDBNull(1) ? null : reader.GetString(1);
                                if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
                                {
                                    Map[code.Trim()] = name.Trim();
                                }
                            }
                        }
                    }
                }

                _loaded = true;
            }
            catch
            {
                // 使用内置 92/95 映射即可
            }
        }

        /// <summary>
        /// 卡内 UsedOilType 字节按十六进制拼成油品编码（如 10 20 → 1020 → 92#汽油）
        /// </summary>
        public static string Decode(byte[] usedOilType, out string codes)
        {
            codes = string.Empty;
            if (usedOilType == null || usedOilType.Length == 0)
            {
                return "不限";
            }

            // 全 FF 表示不限制
            var allFf = true;
            foreach (var b in usedOilType)
            {
                if (b != 0xFF)
                {
                    allFf = false;
                    break;
                }
            }

            if (allFf)
            {
                return "不限";
            }

            var hex = new StringBuilder();
            foreach (var b in usedOilType)
            {
                if (b == 0x00 || b == 0xFF) continue;
                hex.Append(b.ToString("X2"));
            }

            var raw = hex.ToString();
            if (string.IsNullOrEmpty(raw))
            {
                return "不限";
            }

            // 按 4 位一组解析多个油品码
            var names = new List<string>();
            var codeList = new List<string>();
            for (var i = 0; i + 3 < raw.Length; i += 4)
            {
                var code = raw.Substring(i, 4);
                codeList.Add(code);
                names.Add(NormalizeOilName(Map.TryGetValue(code, out var name) ? name : code));
            }

            if (codeList.Count == 0 && raw.Length >= 4)
            {
                var code = raw.Substring(0, 4);
                codeList.Add(code);
                names.Add(NormalizeOilName(Map.TryGetValue(code, out var name) ? name : code));
            }

            codes = string.Join(",", codeList);
            return names.Count == 0 ? raw : string.Join("/", names);
        }

        private static string NormalizeOilName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return name.Replace("#", "号");
        }
    }
}

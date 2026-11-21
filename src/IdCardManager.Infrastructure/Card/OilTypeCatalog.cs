using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace IdCardManager.Infrastructure.Card
{
    public sealed class OilTypeOption
    {
        public OilTypeOption(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public string Code { get; }
        public string Name { get; }
    }

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

        private static readonly List<string> Order = new List<string>
        {
            "1020", "1040", "1030", "1050", "1090"
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
                                    var trimmedCode = code.Trim();
                                    Map[trimmedCode] = name.Trim();
                                    if (!Order.Contains(trimmedCode, StringComparer.OrdinalIgnoreCase))
                                    {
                                        Order.Add(trimmedCode);
                                    }
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

        /// <summary>可选的油品列表（内置 + 原系统 OilInfo 表）。</summary>
        public static IReadOnlyList<OilTypeOption> Options
        {
            get
            {
                var list = new List<OilTypeOption>();
                foreach (var code in Order)
                {
                    if (Map.TryGetValue(code, out var name))
                    {
                        list.Add(new OilTypeOption(code, NormalizeOilName(name)));
                    }
                }

                foreach (var kv in Map)
                {
                    if (!Order.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        list.Add(new OilTypeOption(kv.Key, NormalizeOilName(kv.Value)));
                    }
                }

                return list;
            }
        }

        public static string GetName(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;
            var trimmed = code.Trim();
            return Map.TryGetValue(trimmed, out var name) ? NormalizeOilName(name) : trimmed;
        }

        /// <summary>
        /// 卡内 UsedOilType 字节按每 2 字节一个油品编码解析；返回空列表表示不限。
        /// </summary>
        public static List<string> DecodeCodes(byte[] usedOilType)
        {
            var result = new List<string>();
            if (usedOilType == null || usedOilType.Length == 0)
            {
                return result;
            }

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
                return result;
            }

            var bytes = new List<byte>();
            foreach (var b in usedOilType)
            {
                if (b != 0x00 && b != 0xFF) bytes.Add(b);
            }

            for (var i = 0; i + 1 < bytes.Count; i += 2)
            {
                result.Add(bytes[i].ToString("X2") + bytes[i + 1].ToString("X2"));
            }

            return result;
        }

        /// <summary>
        /// 按选中的油品编码生成 UsedOilType 字节；空集合表示不限（全部 0xFF）。
        /// length 通常沿用卡里读出来的数组长度。
        /// </summary>
        public static byte[] Encode(IEnumerable<string> codes, int length)
        {
            var selected = new List<string>();
            if (codes != null)
            {
                foreach (var code in codes)
                {
                    if (string.IsNullOrWhiteSpace(code)) continue;
                    var trimmed = code.Trim();
                    if (!selected.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                    {
                        selected.Add(trimmed);
                    }
                }
            }

            var need = selected.Count * 2;
            if (length < need) length = need;
            if (length <= 0) length = 8;

            var buffer = new byte[length];
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0xFF;
            }

            var offset = 0;
            foreach (var code in selected)
            {
                if (code.Length != 4 || !IsHex(code)) continue;
                buffer[offset] = Convert.ToByte(code.Substring(0, 2), 16);
                buffer[offset + 1] = Convert.ToByte(code.Substring(2, 2), 16);
                offset += 2;
            }

            return buffer;
        }

        /// <summary>
        /// 卡内 UsedOilType 字节按十六进制拼成油品编码（如 10 20 → 1020 → 92#汽油）
        /// </summary>
        public static string Decode(byte[] usedOilType, out string codes)
        {
            var codeList = DecodeCodes(usedOilType);
            codes = string.Join(",", codeList);
            if (codeList.Count == 0)
            {
                return "不限";
            }

            var names = new List<string>();
            foreach (var code in codeList)
            {
                names.Add(GetName(code));
            }

            return string.Join("/", names);
        }

        private static bool IsHex(string text)
        {
            foreach (var ch in text)
            {
                var ok = (ch >= '0' && ch <= '9') ||
                         (ch >= 'A' && ch <= 'F') ||
                         (ch >= 'a' && ch <= 'f');
                if (!ok) return false;
            }

            return true;
        }

        private static string NormalizeOilName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return name.Replace("#", "号");
        }
    }
}

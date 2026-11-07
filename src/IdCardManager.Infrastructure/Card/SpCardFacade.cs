using System;
using System.Text;
using System.Threading;
using IdCardManager.Domain.Abstractions;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;
using Lr.NativeWrapper.LrReader;
using Lr.NativeWrapper.SpCardApp;

namespace IdCardManager.Infrastructure.Card
{
    public sealed class SpCardFacade : ICardFacade, IDisposable
    {
        private readonly object _sync = new object();
        private readonly AppSettings _settings;
        private LrReader _reader;
        private bool _opened;
        private DateTime _lastHardReconnectUtc = DateTime.MinValue;
        private const int AutoReconnectCooldownMs = 2500;

        public SpCardFacade(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool Open()
        {
            lock (_sync)
            {
                return OpenCore();
            }
        }

        public void Close()
        {
            lock (_sync)
            {
                ReleaseReaderCore(disposeInstance: false);
            }
        }

        /// <summary>
        /// 硬重连：Close + 丢弃 LrReader 实例 + 稍等 + 新建再 Open。
        /// 比单纯再调 Open 更接近「拔掉 USB 再插上」的恢复效果。
        /// </summary>
        public bool Reconnect()
        {
            lock (_sync)
            {
                return HardReconnectCore();
            }
        }

        public CardSlotStatus GetSlotStatus()
        {
            lock (_sync)
            {
                var status = new CardSlotStatus
                {
                    ReaderState = ReaderConnectionState.Disconnected,
                    AuthCardType = AuthCardType.None,
                    UserCardPresent = false,
                    Message = "读卡器未连接"
                };

                if (!EnsureReaderReady())
                {
                    // 软打开失败：冷却后自动硬重连，避免老人反复拔线/重启程序
                    if (CanAutoHardReconnect() && HardReconnectCore())
                    {
                        // 重连成功，继续填状态
                    }
                    else
                    {
                        return status;
                    }
                }

                status.ReaderState = ReaderConnectionState.Connected;
                status.Message = "读卡器已连接";

                try
                {
                    FillSlotFields(status);
                }
                catch (Exception)
                {
                    if (!HardReconnectCore())
                    {
                        status.ReaderState = ReaderConnectionState.Disconnected;
                        status.Message = "读卡器未连接";
                        return status;
                    }

                    try
                    {
                        FillSlotFields(status);
                        status.ReaderState = ReaderConnectionState.Connected;
                        status.Message = "读卡器已连接";
                    }
                    catch (Exception ex2)
                    {
                        status.ReaderState = ReaderConnectionState.Disconnected;
                        status.Message = "读卡器状态异常:" + Truncate(ex2.Message);
                        // 下次轮询再硬重连
                        ReleaseReaderCore(disposeInstance: true);
                    }
                }

                return status;
            }
        }

        private void FillSlotFields(CardSlotStatus status)
        {
            status.AuthCardType = DetectAuthCardType(_settings.AuthCardNad);
            status.UserCardPresent = IsOilCardPresent(_settings.UserCardNad);
            if (status.UserCardPresent)
            {
                status.UserCardSerialNo = TryReadSerial(_settings.UserCardNad);
            }
        }

        private bool EnsureReaderReady()
        {
            if (_opened && _reader != null)
            {
                return true;
            }

            return OpenCore();
        }

        private bool CanAutoHardReconnect()
        {
            return (DateTime.UtcNow - _lastHardReconnectUtc).TotalMilliseconds >= AutoReconnectCooldownMs;
        }

        private bool HardReconnectCore()
        {
            _lastHardReconnectUtc = DateTime.UtcNow;
            ReleaseReaderCore(disposeInstance: true);
            try
            {
                // 给 USB/驱动一点喘息时间，提高二次 Open 成功率
                Thread.Sleep(400);
            }
            catch
            {
                // ignore
            }

            return OpenCore();
        }

        private bool OpenCore()
        {
            try
            {
                if (_reader == null)
                {
                    _reader = new LrReader(_settings.ReaderType, _settings.ReaderPort);
                }

                _opened = _reader.Open();
                if (!_opened)
                {
                    // Open 失败时丢弃实例，避免下次仍用坏句柄
                    ReleaseReaderCore(disposeInstance: true);
                }

                return _opened;
            }
            catch
            {
                ReleaseReaderCore(disposeInstance: true);
                return false;
            }
        }

        private void ReleaseReaderCore(bool disposeInstance)
        {
            try
            {
                _reader?.Close();
            }
            catch
            {
                // ignore
            }

            _opened = false;
            if (disposeInstance)
            {
                _reader = null;
            }
        }

        public CardSnapshot ReadUserCard(string pin)
        {
            lock (_sync)
            {
                EnsureOpen();
                var snap = new CardSnapshot { DetectedCardType = "OilCard" };
                try
                {
                    using (var oil = new OilCard(_reader, _settings.UserCardNad))
                    {
                        oil.CheckCard(out var st);
                        snap.CardStep = StatusName(st);
                        if (st == CardStatusConstants.CHECKCARD_STATUS_NOCARD)
                        {
                            snap.ExtraFields["错误"] = "请插入用户卡";
                            return snap;
                        }

                        if (st == CardStatusConstants.CHECKCARD_STATUS_WRONGCARD)
                        {
                            snap.ExtraFields["错误"] = "卡类型错误";
                            return snap;
                        }

                        oil.ReadPubAppFile(out var pub);
                        snap.IssuerId = ToHex(pub.IssuerID);
                        snap.AppTypeId = pub.AppTypeID.ToString("X2");
                        snap.AppVer = pub.AppVer.ToString("X2");
                        snap.AppSerialNo = BytesToAscii(pub.SerialNo);
                        snap.EnablementDate = BytesToAscii(pub.EnablementDate);
                        snap.ExpirationDate = BytesToAscii(pub.ExpirationDate);
                        snap.InstructionSetVer = pub.InstructionSetVer.ToString("X2");
                        snap.Fci = pub.FCI.ToString("X2");
                        snap.PubAppHex = ToHex(Concat(pub.IssuerID, new[] { pub.AppTypeID, pub.AppVer }, pub.SerialNo, pub.EnablementDate, pub.ExpirationDate, new[] { pub.InstructionSetVer, pub.FCI }));

                        oil.ReadCommonFile(out var common);
                        snap.UseDefaultPassword = common.UseDefaultPassword == 0 ? "否" : "是(" + common.UseDefaultPassword + ")";
                        snap.EmployeeNo = common.EmployeeNo.ToString();
                        snap.EmployeePasswordHex = ToHex(common.EmployeePassword);
                        snap.CommonHex = ToHex(Concat(new[] { common.UseDefaultPassword, common.EmployeeNo }, common.EmployeePassword, common.rfu));

                        oil.ReadPrivateFile(out var priv);
                        snap.CardTypeCode = priv.CardType.ToString();
                        snap.EmployeeId = priv.EmployeeID.ToString();
                        snap.HolderName = BytesToGbk(priv.name);
                        snap.CertificateNo = BytesToGbk(priv.CertificateNo);
                        snap.CertificateType = CertTypeName(priv.CertificateType);
                        snap.PrivateHex = ToHex(Concat(new[] { priv.CardType, priv.EmployeeID }, priv.name, priv.CertificateNo, new[] { priv.CertificateType }));

                        oil.ReadSensitiveFile(out var sens);
                        snap.OilTypeHex = ToHex(sens.UsedOilType);
                        snap.OilTypeCode = OilTypeCatalog.Decode(sens.UsedOilType, out _);
                        snap.AreaMode = sens.UsedAreaMode.ToString();
                        snap.AreaLimitHex = ToHex(sens.UsedArea);
                        snap.AreaLimit = BytesToAscii(sens.UsedArea);
                        snap.PerTimeAmountHex = ToHex(sens.UsedAmount);
                        snap.PerTimeAmount = FormatMoneyBytes(sens.UsedAmount);
                        snap.PerDayTimes = sens.UsedFrequency.ToString();
                        snap.PerDayAmountHex = ToHex(sens.UsedTotalMoney);
                        snap.PerDayAmount = FormatMoneyBytes(sens.UsedTotalMoney);
                        snap.PlateNumber = BytesToGbk(sens.UsedMotorcar);
                        snap.PriceIndex = sens.PriceIndex.ToString();
                        snap.SensitiveHex = ToHex(Concat(sens.UsedOilType, new[] { sens.UsedAreaMode }, sens.UsedArea, sens.UsedAmount, new[] { sens.UsedFrequency }, sens.UsedTotalMoney, sens.UsedMotorcar, sens.rfu, new[] { sens.PriceIndex }));

                        if (!string.IsNullOrWhiteSpace(pin))
                        {
                            var pinText = pin.Trim();
                            try
                            {
                                oil.GetBalance(pinText, out var bal);
                                snap.BalanceFen = bal;
                            }
                            catch (Exception ex)
                            {
                                snap.ExtraFields["余额"] = Truncate(ex.Message);
                            }

                            try
                            {
                                oil.IsGreyCard(pinText, out var grey);
                                snap.IsGreyCard = grey ? "是" : "否";
                            }
                            catch (Exception ex)
                            {
                                snap.IsGreyCard = "未知";
                                snap.ExtraFields["灰卡检测"] = Truncate(ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    snap.ExtraFields["错误"] = Truncate(ex.Message);
                }

                return snap;
            }
        }

        public uint? GetBalance(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
            {
                return null;
            }

            lock (_sync)
            {
                EnsureOpen();
                using (var oil = new OilCard(_reader, _settings.UserCardNad))
                {
                    oil.CheckCard(out var st);
                    if (st == CardStatusConstants.CHECKCARD_STATUS_NOCARD ||
                        st == CardStatusConstants.CHECKCARD_STATUS_WRONGCARD)
                    {
                        return null;
                    }

                    oil.GetBalance(pin.Trim(), out var bal);
                    return bal;
                }
            }
        }

        public CardUnlockResult Unlock(string pin)
        {
            var result = new CardUnlockResult();
            if (string.IsNullOrWhiteSpace(pin))
            {
                result.ErrorMessage = "请输入用户卡密码";
                return result;
            }

            lock (_sync)
            {
                try
                {
                    EnsureOpen();
                    using (var oil = new OilCard(_reader, _settings.UserCardNad))
                    {
                        oil.CheckCard(out var st);
                        if (st == CardStatusConstants.CHECKCARD_STATUS_NOCARD)
                        {
                            result.ErrorMessage = "请插入用户卡";
                            return result;
                        }

                        if (st == CardStatusConstants.CHECKCARD_STATUS_WRONGCARD)
                        {
                            result.ErrorMessage = "请插入用户卡";
                            return result;
                        }

                        var pinText = pin.Trim();
                        oil.VerifyPin(pinText);

                        oil.ReadPubAppFile(out var pub);
                        result.CardSerialNo = BytesToAscii(pub.SerialNo);

                        oil.GetBalance(pinText, out var bal);
                        result.BalanceFen = bal;

                        oil.ReadSensitiveFile(out var sens);
                        result.OilLimitText = OilTypeCatalog.Decode(sens.UsedOilType, out var codes);
                        result.OilLimitCodes = codes;

                        // 同步写回快照字段，便于卡片数据页
                        result.Success = true;
                        return result;
                    }
                }
                catch (Exception)
                {
                    result.Success = false;
                    result.ErrorMessage = "密码错误";
                    return result;
                }
            }
        }

        public StoreWithdrawResult ResetPin(string newPin, AppSettings settings)
        {
            var result = new StoreWithdrawResult();
            if (string.IsNullOrWhiteSpace(newPin))
            {
                result.ErrorMessage = "请输入新密码";
                return result;
            }

            if (settings == null || string.IsNullOrWhiteSpace(settings.UserKey))
            {
                result.ErrorMessage = "请到参数设置导入密钥";
                return result;
            }

            lock (_sync)
            {
                try
                {
                    EnsureOpen();
                    using (var oil = new OilCard(_reader, settings.UserCardNad))
                    using (var value = new ValueCard(_reader, settings.AuthCardNad))
                    {
                        value.CheckCard(out var authStatus);
                        if (authStatus == CardStatusConstants.CHECKCARD_STATUS_NOCARD ||
                            authStatus == CardStatusConstants.CHECKCARD_STATUS_WRONGCARD)
                        {
                            result.ErrorMessage = "请插入授权卡";
                            return result;
                        }

                        oil.CheckCard(out var oilStatus);
                        if (oilStatus == CardStatusConstants.CHECKCARD_STATUS_NOCARD)
                        {
                            result.ErrorMessage = "请插入用户卡";
                            return result;
                        }

                        // 原厂 API：授权卡 + 系统密钥重置用户卡 PIN（无法回读旧密码）
                        oil.ReloadPin(value, settings.UserKey, newPin.Trim());
                        result.Success = true;
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = Truncate(ex.Message);
                    return result;
                }
            }
        }

        public StoreWithdrawResult Store(int amountFen, string pin, AppSettings settings)
        {
            return MutateBalance(amountFen, pin, settings, store: true);
        }

        public StoreWithdrawResult Withdraw(int amountFen, string pin, AppSettings settings)
        {
            return MutateBalance(amountFen, pin, settings, store: false);
        }

        private StoreWithdrawResult MutateBalance(int amountFen, string pin, AppSettings settings, bool store)
        {
            lock (_sync)
            {
                EnsureOpen();
                var result = new StoreWithdrawResult();
                try
                {
                    using (var oil = new OilCard(_reader, settings.UserCardNad))
                    using (var value = new ValueCard(_reader, settings.AuthCardNad))
                    {
                        value.CheckCard(out var authStatus);
                        if (authStatus == CardStatusConstants.CHECKCARD_STATUS_NOCARD ||
                            authStatus == CardStatusConstants.CHECKCARD_STATUS_WRONGCARD)
                        {
                            result.Success = false;
                            result.ErrorMessage = "请插入授权卡";
                            return result;
                        }

                        oil.CheckCard(out var oilStatus);
                        if (oilStatus == CardStatusConstants.CHECKCARD_STATUS_NOCARD ||
                            oilStatus == CardStatusConstants.CHECKCARD_STATUS_WRONGCARD)
                        {
                            result.Success = false;
                            result.ErrorMessage = "请插入用户卡";
                            return result;
                        }

                        oil.ReadPubAppFile(out var pub);
                        result.CardSerialNo = BytesToAscii(pub.SerialNo);

                        if (store)
                        {
                            oil.CreditForLoad(value, settings.UserKey, (uint)amountFen, pin);
                        }
                        else
                        {
                            oil.CreditForUnLoad(value, settings.UserKey, (uint)amountFen, pin);
                        }

                        try
                        {
                            oil.GetBalance(pin, out var bal);
                            result.BalanceAfterFen = bal;
                        }
                        catch
                        {
                            // 余额回读失败不影响主成功
                        }

                        result.Success = true;
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = Truncate(ex.Message);
                    return result;
                }
            }
        }

        private AuthCardType DetectAuthCardType(int nad)
        {
            // 轮询只认 ValueCard（充/扣授权卡）。
            // 不再每次轮询 new MotherCard：母卡 CheckCard 易把读卡器句柄打坏，表现为「读卡器未连接」。
            int statusCode;
            using (var c = new ValueCard(_reader, nad))
            {
                c.CheckCard(out statusCode);
            }

            if (IsPresentValid(statusCode))
            {
                return AuthCardType.ValueCard;
            }

            // 槽内有卡但不是充值授权卡（母卡等）— 不再轮询 MotherCard，避免打坏读卡器
            if (statusCode == CardStatusConstants.CHECKCARD_STATUS_WRONGCARD)
            {
                return AuthCardType.Unknown;
            }

            return AuthCardType.None;
        }

        private bool IsOilCardPresent(int nad)
        {
            return TryCheck(() =>
            {
                using (var c = new OilCard(_reader, nad))
                {
                    c.CheckCard(out var s);
                    return s != CardStatusConstants.CHECKCARD_STATUS_NOCARD &&
                           s != CardStatusConstants.CHECKCARD_STATUS_WRONGCARD;
                }
            });
        }

        private string TryReadSerial(int nad)
        {
            try
            {
                using (var oil = new OilCard(_reader, nad))
                {
                    oil.ReadPubAppFile(out var pub);
                    return BytesToAscii(pub.SerialNo);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private void EnsureOpen()
        {
            if (_opened && _reader != null)
            {
                return;
            }

            if (!OpenCore() && !HardReconnectCore())
            {
                throw new InvalidOperationException("读卡器未连接");
            }
        }

        private static bool IsPresentValid(int status)
        {
            return status != CardStatusConstants.CHECKCARD_STATUS_NOCARD &&
                   status != CardStatusConstants.CHECKCARD_STATUS_WRONGCARD;
        }

        private static bool TryCheck(Func<bool> action)
        {
            try
            {
                return action();
            }
            catch
            {
                return false;
            }
        }

        private static string StatusName(int status)
        {
            if (status == CardStatusConstants.CHECKCARD_STATUS_NOCARD) return "无卡";
            if (status == CardStatusConstants.CHECKCARD_STATUS_NOINIT) return "未初始化";
            if (status == CardStatusConstants.CHECKCARD_STATUS_INIT) return "已初始化";
            if (status == CardStatusConstants.CHECKCARD_STATUS_WRONGCARD) return "卡类型错误";
            if (status == CardStatusConstants.CHECKCARD_STATUS_FORMATED) return "已格式化";
            if (status == CardStatusConstants.CHECKCARD_STATUS_SALED) return "已售出";
            return status.ToString();
        }

        private static string BytesToAscii(byte[] data)
        {
            if (data == null || data.Length == 0) return string.Empty;
            var sb = new StringBuilder();
            foreach (var b in data)
            {
                if (b == 0) break;
                if (b >= 32 && b < 127) sb.Append((char)b);
                else sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        private static string BytesToGbk(byte[] data)
        {
            if (data == null || data.Length == 0) return string.Empty;
            try
            {
                var enc = Encoding.GetEncoding("GB2312");
                return enc.GetString(data).Trim('\0', ' ', '\u3000');
            }
            catch
            {
                return ToHex(data);
            }
        }

        private static string ToHex(byte[] data)
        {
            if (data == null) return string.Empty;
            var sb = new StringBuilder(data.Length * 2);
            foreach (var b in data) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        private static string Truncate(string message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;
            message = message.Replace("。", string.Empty).Replace(".", string.Empty);
            return message.Length <= 120 ? message : message.Substring(0, 120);
        }

        private static string CertTypeName(byte type)
        {
            switch (type)
            {
                case 0: return "未设置";
                case 1: return "身份证";
                case 2: return "军官证";
                case 3: return "护照";
                default: return type.ToString();
            }
        }

        private static string FormatMoneyBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return string.Empty;
            // 常见为 4 字节大端金额(分) 或 BCD；优先按无符号整型解析
            if (data.Length >= 4)
            {
                uint fen = ((uint)data[0] << 24) | ((uint)data[1] << 16) | ((uint)data[2] << 8) | data[3];
                if (fen > 0 && fen < 100000000)
                {
                    return (fen / 100.0).ToString("0.00") + "元";
                }
            }

            return ToHex(data);
        }

        private static byte[] Concat(params byte[][] parts)
        {
            var len = 0;
            foreach (var p in parts)
            {
                if (p != null) len += p.Length;
            }

            var buf = new byte[len];
            var offset = 0;
            foreach (var p in parts)
            {
                if (p == null || p.Length == 0) continue;
                Buffer.BlockCopy(p, 0, buf, offset, p.Length);
                offset += p.Length;
            }

            return buf;
        }

        public void Dispose()
        {
            lock (_sync)
            {
                ReleaseReaderCore(disposeInstance: true);
            }
        }
    }
}


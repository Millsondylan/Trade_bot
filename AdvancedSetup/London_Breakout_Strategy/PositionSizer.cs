using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace TradingBot.Framework.RiskManagement
{
    /// <summary>
    /// Advanced position sizing system implementing multiple methodologies:
    /// - Fixed percentage risk
    /// - ATR-based dynamic sizing
    /// - Kelly Criterion (fractional)
    /// - Volatility-adjusted sizing
    /// </summary>
    public class PositionSizer
    {
        private readonly Robot _robot;
        private readonly Symbol _symbol;

        public PositionSizer(Robot robot, Symbol symbol)
        {
            _robot = robot;
            _symbol = symbol;
        }

        /// <summary>
        /// Calculate position size based on fixed percentage risk
        /// This is the most common and safest approach for retail traders
        /// </summary>
        /// <param name="riskPercent">Percentage of account to risk (0.5-2% recommended)</param>
        /// <param name="stopLossPips">Stop loss distance in pips</param>
        /// <returns>Position size in units</returns>
        public double CalculatePositionSizeByRisk(double riskPercent, double stopLossPips)
        {
            if (stopLossPips <= 0)
            {
                _robot.Print($"[PositionSizer] ERROR: Invalid stop loss: {stopLossPips} pips");
                return 0;
            }

            // Calculate risk amount in account currency
            double accountRisk = _robot.Account.Balance * (riskPercent / 100.0);

            // Calculate position size
            double positionSize = accountRisk / (stopLossPips * _symbol.PipValue);

            // Normalize to valid lot size
            double normalizedSize = _symbol.NormalizeVolumeInUnits(positionSize, RoundingMode.Down);

            _robot.Print($"[PositionSizer] Fixed Risk - Risk: {riskPercent}%, SL: {stopLossPips} pips, Size: {normalizedSize} units");

            return normalizedSize;
        }

        /// <summary>
        /// Calculate position size based on ATR (Average True Range)
        /// Automatically adjusts to market volatility - smaller positions in volatile markets
        /// </summary>
        /// <param name="riskPercent">Percentage of account to risk</param>
        /// <param name="atr">Current ATR value (use ATR indicator)</param>
        /// <param name="atrMultiplier">Multiplier for stop loss distance (2-3 for trending, 1-2 for ranging)</param>
        /// <returns>Position size in units</returns>
        public double CalculatePositionSizeByATR(double riskPercent, double atr, double atrMultiplier)
        {
            if (atr <= 0)
            {
                _robot.Print($"[PositionSizer] ERROR: Invalid ATR: {atr}");
                return 0;
            }

            // Calculate account risk
            double accountRisk = _robot.Account.Balance * (riskPercent / 100.0);

            // Calculate stop loss in pips based on ATR
            double stopLossPips = (atr * atrMultiplier) / _symbol.PipSize;

            // Calculate position size
            double positionSize = accountRisk / (stopLossPips * _symbol.PipValue);

            // Normalize to valid lot size
            double normalizedSize = _symbol.NormalizeVolumeInUnits(positionSize, RoundingMode.Down);

            _robot.Print($"[PositionSizer] ATR-Based - Risk: {riskPercent}%, ATR: {atr:F5}, SL: {stopLossPips:F1} pips, Size: {normalizedSize} units");

            return normalizedSize;
        }

        /// <summary>
        /// Calculate position size using Kelly Criterion
        /// WARNING: Use fractional Kelly (25-50%) to avoid over-leveraging
        /// Requires accurate win rate and risk/reward data from backtesting
        /// </summary>
        /// <param name="winRate">Historical win rate (0.0 to 1.0, e.g., 0.60 for 60%)</param>
        /// <param name="avgWinLossRatio">Average win/loss ratio (e.g., 1.5 means wins are 1.5x losses)</param>
        /// <param name="kellyFraction">Fraction of Kelly to use (0.25-0.5 recommended, 1.0 = full Kelly)</param>
        /// <param name="stopLossPips">Stop loss distance in pips</param>
        /// <returns>Position size in units</returns>
        public double CalculatePositionSizeByKelly(double winRate, double avgWinLossRatio, double kellyFraction, double stopLossPips)
        {
            if (winRate <= 0 || winRate >= 1.0)
            {
                _robot.Print($"[PositionSizer] ERROR: Invalid win rate: {winRate}");
                return 0;
            }

            if (avgWinLossRatio <= 0)
            {
                _robot.Print($"[PositionSizer] ERROR: Invalid win/loss ratio: {avgWinLossRatio}");
                return 0;
            }

            // Kelly Criterion formula: f* = [p(W+1) - 1] / W
            // where p = win probability, W = win/loss ratio
            double kellyPercent = ((winRate * (avgWinLossRatio + 1)) - 1) / avgWinLossRatio;

            // Apply fractional Kelly for safety
            double adjustedKelly = kellyPercent * kellyFraction;

            // Cap at reasonable maximum (10% of account)
            if (adjustedKelly > 0.10)
            {
                _robot.Print($"[PositionSizer] WARNING: Kelly suggests {adjustedKelly * 100:F1}%, capping at 10%");
                adjustedKelly = 0.10;
            }

            // Ensure minimum is reasonable
            if (adjustedKelly < 0.001)
            {
                _robot.Print($"[PositionSizer] WARNING: Kelly too small, using 0.5% minimum");
                adjustedKelly = 0.005;
            }

            // Use fixed risk calculation with Kelly percentage
            double positionSize = CalculatePositionSizeByRisk(adjustedKelly * 100, stopLossPips);

            _robot.Print($"[PositionSizer] Kelly - WinRate: {winRate * 100:F1}%, W/L: {avgWinLossRatio:F2}, " +
                        $"Kelly: {kellyPercent * 100:F1}%, Fractional: {adjustedKelly * 100:F1}%");

            return positionSize;
        }

        /// <summary>
        /// Calculate maximum position size based on account and broker constraints
        /// </summary>
        /// <param name="maxRiskPercent">Maximum percentage of account to risk</param>
        /// <returns>Maximum position size in units</returns>
        public double GetMaxPositionSize(double maxRiskPercent = 5.0)
        {
            // Calculate maximum position value
            double maxPositionValue = _robot.Account.Equity * (maxRiskPercent / 100.0);

            // Convert to units (simplified - assumes 1:1 for demonstration)
            double maxUnits = maxPositionValue / (_symbol.Bid * _symbol.LotSize);

            // Normalize
            double normalizedMax = _symbol.NormalizeVolumeInUnits(maxUnits, RoundingMode.Down);

            return normalizedMax;
        }

        /// <summary>
        /// Calculate stop loss in pips based on price levels
        /// </summary>
        /// <param name="entryPrice">Entry price</param>
        /// <param name="stopLossPrice">Stop loss price</param>
        /// <returns>Stop loss distance in pips</returns>
        public double CalculateStopLossPips(double entryPrice, double stopLossPrice)
        {
            double priceDifference = Math.Abs(entryPrice - stopLossPrice);
            double pips = priceDifference / _symbol.PipSize;
            return pips;
        }

        /// <summary>
        /// Calculate take profit price based on risk-reward ratio
        /// </summary>
        /// <param name="entryPrice">Entry price</param>
        /// <param name="stopLossPrice">Stop loss price</param>
        /// <param name="riskRewardRatio">Desired risk-reward ratio (e.g., 2.0 for 1:2)</param>
        /// <param name="tradeType">Buy or Sell</param>
        /// <returns>Take profit price</returns>
        public double CalculateTakeProfitPrice(double entryPrice, double stopLossPrice, double riskRewardRatio, TradeType tradeType)
        {
            double stopDistance = Math.Abs(entryPrice - stopLossPrice);
            double takeProfitDistance = stopDistance * riskRewardRatio;

            double takeProfitPrice;
            if (tradeType == TradeType.Buy)
            {
                takeProfitPrice = entryPrice + takeProfitDistance;
            }
            else
            {
                takeProfitPrice = entryPrice - takeProfitDistance;
            }

            return takeProfitPrice;
        }

        /// <summary>
        /// Validates if position size is within safe limits
        /// </summary>
        /// <param name="positionSize">Proposed position size in units</param>
        /// <param name="maxRiskPercent">Maximum allowed risk percentage</param>
        /// <returns>True if safe, false if exceeds limits</returns>
        public bool ValidatePositionSize(double positionSize, double maxRiskPercent = 5.0)
        {
            if (positionSize <= 0)
            {
                _robot.Print("[PositionSizer] ERROR: Position size must be positive");
                return false;
            }

            // Check against minimum
            if (positionSize < _symbol.VolumeInUnitsMin)
            {
                _robot.Print($"[PositionSizer] ERROR: Position size {positionSize} below minimum {_symbol.VolumeInUnitsMin}");
                return false;
            }

            // Check against maximum
            if (positionSize > _symbol.VolumeInUnitsMax)
            {
                _robot.Print($"[PositionSizer] ERROR: Position size {positionSize} exceeds maximum {_symbol.VolumeInUnitsMax}");
                return false;
            }

            // Check if position value exceeds max risk
            double positionValue = positionSize * _symbol.Bid;
            double accountEquity = _robot.Account.Equity;
            double positionRiskPercent = (positionValue / accountEquity) * 100;

            if (positionRiskPercent > maxRiskPercent)
            {
                _robot.Print($"[PositionSizer] WARNING: Position represents {positionRiskPercent:F2}% of equity (max: {maxRiskPercent}%)");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get position size information for logging and debugging
        /// </summary>
        public string GetPositionSizeInfo(double positionSize)
        {
            double lots = positionSize / _symbol.LotSize;
            double positionValue = positionSize * _symbol.Bid;
            double percentOfEquity = (positionValue / _robot.Account.Equity) * 100;

            return $"Size: {positionSize} units ({lots:F2} lots), " +
                   $"Value: {positionValue:F2}, " +
                   $"Equity%: {percentOfEquity:F2}%";
        }
    }
}

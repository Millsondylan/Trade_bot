using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;

namespace TradingBot.Framework.ErrorHandling
{
    /// <summary>
    /// Comprehensive error handling system for live trading
    /// Implements retry logic, circuit breakers, and error recovery
    /// </summary>
    public class ErrorHandler
    {
        private readonly Robot _robot;
        private readonly int _maxRetries;
        private readonly Dictionary<ErrorCode, int> _errorCounts;
        private readonly Dictionary<ErrorCode, DateTime> _lastErrorTime;
        private int _consecutiveErrors;
        private DateTime _lastSuccessfulTrade;

        public ErrorHandler(Robot robot, int maxRetries = 3)
        {
            _robot = robot;
            _maxRetries = maxRetries;
            _errorCounts = new Dictionary<ErrorCode, int>();
            _lastErrorTime = new Dictionary<ErrorCode, DateTime>();
            _consecutiveErrors = 0;
            _lastSuccessfulTrade = robot.Server.Time;
        }

        /// <summary>
        /// Handle trade execution result with automatic retry logic
        /// </summary>
        public bool HandleTradeResult(TradeResult result, string operationDescription)
        {
            if (result.IsSuccessful)
            {
                _consecutiveErrors = 0;
                _lastSuccessfulTrade = _robot.Server.Time;
                return true;
            }

            // Log the error
            ErrorCode errorCode = result.Error.Value;
            LogError(errorCode, operationDescription);

            // Handle specific error types
            switch (errorCode)
            {
                case ErrorCode.NoMoney:
                    _robot.Print($"[ErrorHandler] CRITICAL: Insufficient funds for {operationDescription}");
                    _robot.Print("[ErrorHandler] Reduce position size or deposit more funds");
                    return false;

                case ErrorCode.MarketClosed:
                    _robot.Print($"[ErrorHandler] Market closed for {operationDescription}");
                    _robot.Print("[ErrorHandler] Check trading hours and retry later");
                    return false;

                case ErrorCode.Disconnected:
                    _robot.Print($"[ErrorHandler] Disconnected during {operationDescription}");
                    return HandleDisconnection();

                case ErrorCode.Timeout:
                    _robot.Print($"[ErrorHandler] Timeout during {operationDescription}");
                    return HandleTimeout();

                case ErrorCode.BadVolume:
                    _robot.Print($"[ErrorHandler] Invalid volume for {operationDescription}");
                    _robot.Print($"[ErrorHandler] Check min/max volume constraints");
                    return false;

                case ErrorCode.TechnicalError:
                    _robot.Print($"[ErrorHandler] Technical error during {operationDescription}");
                    return HandleTechnicalError();

                case ErrorCode.BadStopLoss:
                case ErrorCode.BadTakeProfit:
                    _robot.Print($"[ErrorHandler] Invalid SL/TP for {operationDescription}");
                    _robot.Print("[ErrorHandler] Check price levels are outside spread");
                    return false;

                default:
                    _robot.Print($"[ErrorHandler] Unknown error: {errorCode} - {operationDescription}");
                    return false;
            }
        }

        /// <summary>
        /// Execute operation with retry logic and exponential backoff
        /// </summary>
        public TradeResult ExecuteWithRetry(Func<TradeResult> operation, string operationDescription)
        {
            TradeResult result = null;
            int retryCount = 0;

            while (retryCount <= _maxRetries)
            {
                try
                {
                    result = operation();

                    if (result.IsSuccessful)
                    {
                        if (retryCount > 0)
                        {
                            _robot.Print($"[ErrorHandler] Operation succeeded after {retryCount} retries");
                        }
                        _consecutiveErrors = 0;
                        return result;
                    }

                    // Check if error is retryable
                    if (!IsRetryableError(result.Error.Value))
                    {
                        _robot.Print($"[ErrorHandler] Non-retryable error: {result.Error.Value}");
                        return result;
                    }

                    // Exponential backoff: 2s, 4s, 8s
                    int delayMs = (int)(Math.Pow(2, retryCount) * 1000);
                    _robot.Print($"[ErrorHandler] Retry {retryCount + 1}/{_maxRetries} after {delayMs}ms delay");

                    System.Threading.Thread.Sleep(delayMs);
                    retryCount++;
                }
                catch (Exception ex)
                {
                    _robot.Print($"[ErrorHandler] Exception during {operationDescription}: {ex.Message}");
                    retryCount++;

                    if (retryCount > _maxRetries)
                    {
                        throw;
                    }
                }
            }

            _consecutiveErrors++;
            return result;
        }

        /// <summary>
        /// Check if circuit breaker should trip (too many consecutive errors)
        /// </summary>
        public bool ShouldTripCircuitBreaker()
        {
            const int maxConsecutiveErrors = 10;

            if (_consecutiveErrors >= maxConsecutiveErrors)
            {
                _robot.Print($"[ErrorHandler] CIRCUIT BREAKER TRIPPED: {_consecutiveErrors} consecutive errors");
                _robot.Print("[ErrorHandler] Trading halted for safety. Manual review required.");
                return true;
            }

            // Also trip if no successful trades for extended period
            TimeSpan timeSinceSuccess = _robot.Server.Time - _lastSuccessfulTrade;
            if (timeSinceSuccess.TotalHours > 24 && _consecutiveErrors > 5)
            {
                _robot.Print($"[ErrorHandler] CIRCUIT BREAKER: No successful trades for {timeSinceSuccess.TotalHours:F1} hours");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get error statistics for monitoring
        /// </summary>
        public ErrorStatistics GetErrorStatistics()
        {
            return new ErrorStatistics
            {
                ConsecutiveErrors = _consecutiveErrors,
                TotalErrorTypes = _errorCounts.Count,
                TimeSinceLastSuccess = _robot.Server.Time - _lastSuccessfulTrade,
                ErrorCounts = new Dictionary<ErrorCode, int>(_errorCounts)
            };
        }

        /// <summary>
        /// Reset error counters (use after manual intervention)
        /// </summary>
        public void ResetErrorCounters()
        {
            _consecutiveErrors = 0;
            _errorCounts.Clear();
            _lastErrorTime.Clear();
            _robot.Print("[ErrorHandler] Error counters reset");
        }

        private void LogError(ErrorCode errorCode, string description)
        {
            // Increment error count
            if (!_errorCounts.ContainsKey(errorCode))
            {
                _errorCounts[errorCode] = 0;
            }
            _errorCounts[errorCode]++;

            // Record time
            _lastErrorTime[errorCode] = _robot.Server.Time;

            // Increment consecutive errors
            _consecutiveErrors++;

            _robot.Print($"[ErrorHandler] Error #{_consecutiveErrors}: {errorCode} - {description}");
        }

        private bool HandleDisconnection()
        {
            _robot.Print("[ErrorHandler] Attempting to handle disconnection...");

            // Check connection status
            if (!_robot.IsBacktesting)
            {
                _robot.Print("[ErrorHandler] Check internet connection and broker server status");
                // In live trading, may need to wait for reconnection
                return false;
            }

            return false;
        }

        private bool HandleTimeout()
        {
            _robot.Print("[ErrorHandler] Handling timeout with exponential backoff");

            // Timeouts are often temporary - worth retrying
            return true;
        }

        private bool HandleTechnicalError()
        {
            _robot.Print("[ErrorHandler] Technical error - checking system status");

            // Technical errors might be temporary
            // Check if error is recurring
            if (_consecutiveErrors > 3)
            {
                _robot.Print("[ErrorHandler] Recurring technical errors - stopping");
                return false;
            }

            return true;
        }

        private bool IsRetryableError(ErrorCode errorCode)
        {
            switch (errorCode)
            {
                case ErrorCode.Timeout:
                case ErrorCode.Disconnected:
                case ErrorCode.TechnicalError:
                    return true;

                case ErrorCode.NoMoney:
                case ErrorCode.MarketClosed:
                case ErrorCode.BadVolume:
                case ErrorCode.BadStopLoss:
                case ErrorCode.BadTakeProfit:
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Validate order parameters before execution
        /// </summary>
        public bool ValidateOrderParameters(Symbol symbol, double volume, double? stopLoss, double? takeProfit, TradeType tradeType)
        {
            // Check volume
            if (volume < symbol.VolumeInUnitsMin || volume > symbol.VolumeInUnitsMax)
            {
                _robot.Print($"[ErrorHandler] Invalid volume: {volume} (min: {symbol.VolumeInUnitsMin}, max: {symbol.VolumeInUnitsMax})");
                return false;
            }

            // Check stop loss distance
            if (stopLoss.HasValue)
            {
                double currentPrice = tradeType == TradeType.Buy ? symbol.Ask : symbol.Bid;
                double slDistance = Math.Abs(currentPrice - stopLoss.Value);
                double minDistance = symbol.PipSize * 2; // Minimum 2 pips

                if (slDistance < minDistance)
                {
                    _robot.Print($"[ErrorHandler] Stop loss too close: {slDistance / symbol.PipSize:F1} pips (min: 2 pips)");
                    return false;
                }
            }

            // Check take profit distance
            if (takeProfit.HasValue)
            {
                double currentPrice = tradeType == TradeType.Buy ? symbol.Ask : symbol.Bid;
                double tpDistance = Math.Abs(currentPrice - takeProfit.Value);
                double minDistance = symbol.PipSize * 2; // Minimum 2 pips

                if (tpDistance < minDistance)
                {
                    _robot.Print($"[ErrorHandler] Take profit too close: {tpDistance / symbol.PipSize:F1} pips (min: 2 pips)");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Safe position modification with error handling
        /// </summary>
        public bool ModifyPositionSafely(Position position, double? newStopLoss, double? newTakeProfit)
        {
            if (position == null)
            {
                _robot.Print("[ErrorHandler] Cannot modify null position");
                return false;
            }

            try
            {
                var result = ExecuteWithRetry(
                    () => position.ModifyStopLossPips(newStopLoss, newTakeProfit),
                    $"Modify position {position.Id}"
                );

                return HandleTradeResult(result, $"Position modification {position.Id}");
            }
            catch (Exception ex)
            {
                _robot.Print($"[ErrorHandler] Exception modifying position: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safe position close with error handling
        /// </summary>
        public bool ClosePositionSafely(Position position, string reason)
        {
            if (position == null)
            {
                _robot.Print("[ErrorHandler] Cannot close null position");
                return false;
            }

            try
            {
                var result = ExecuteWithRetry(
                    () => position.Close(),
                    $"Close position {position.Id}"
                );

                bool success = HandleTradeResult(result, $"Position close {position.Id}: {reason}");

                if (success)
                {
                    _robot.Print($"[ErrorHandler] Successfully closed position {position.Id}: {reason}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _robot.Print($"[ErrorHandler] Exception closing position: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Error statistics for monitoring
    /// </summary>
    public class ErrorStatistics
    {
        public int ConsecutiveErrors { get; set; }
        public int TotalErrorTypes { get; set; }
        public TimeSpan TimeSinceLastSuccess { get; set; }
        public Dictionary<ErrorCode, int> ErrorCounts { get; set; }
    }
}

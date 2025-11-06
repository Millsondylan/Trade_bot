using System;
using System.Collections.Generic;
using System.Linq;

namespace TradingBot.Utilities.Backtesting
{
    /// <summary>
    /// Validates backtest results and checks for overfitting
    /// Implements the validation protocols from the trading guide
    /// </summary>
    public class BacktestValidator
    {
        /// <summary>
        /// Validates backtest results and returns warnings/errors
        /// </summary>
        public static ValidationResult ValidateBacktest(BacktestResults results, BacktestSettings settings)
        {
            var validation = new ValidationResult();

            // Rule 1: Minimum trade count
            if (results.TotalTrades < 30)
            {
                validation.AddError($"Insufficient trades: {results.TotalTrades} (minimum 30 required for statistical significance)");
            }
            else if (results.TotalTrades < 100)
            {
                validation.AddWarning($"Trade count: {results.TotalTrades} (100+ recommended for robustness)");
            }

            // Rule 2: Unrealistic win rate
            if (results.WinRate > 0.85)
            {
                validation.AddError($"Win rate too high: {results.WinRate * 100:F1}% (likely overfit, expect 55-70% max)");
            }

            // Rule 3: Profit factor validation
            if (results.ProfitFactor < 1.0)
            {
                validation.AddError($"Negative profit factor: {results.ProfitFactor:F2} (strategy loses money)");
            }
            else if (results.ProfitFactor > 5.0)
            {
                validation.AddError($"Profit factor too high: {results.ProfitFactor:F2} (likely overfit, expect 1.5-2.5)");
            }
            else if (results.ProfitFactor < 1.5)
            {
                validation.AddWarning($"Low profit factor: {results.ProfitFactor:F2} (below recommended 1.5+)");
            }

            // Rule 4: Sharpe ratio validation
            if (results.SharpeRatio < 0)
            {
                validation.AddError($"Negative Sharpe ratio: {results.SharpeRatio:F2} (strategy underperforms risk-free rate)");
            }
            else if (results.SharpeRatio < 1.0)
            {
                validation.AddWarning($"Low Sharpe ratio: {results.SharpeRatio:F2} (below 1.0, poor risk-adjusted returns)");
            }
            else if (results.SharpeRatio > 3.0)
            {
                validation.AddWarning($"Very high Sharpe ratio: {results.SharpeRatio:F2} (may indicate overfitting)");
            }

            // Rule 5: Maximum drawdown validation
            if (results.MaxDrawdownPercent > 50)
            {
                validation.AddError($"Excessive drawdown: {results.MaxDrawdownPercent:F1}% (above 50%, high risk of ruin)");
            }
            else if (results.MaxDrawdownPercent > 30)
            {
                validation.AddWarning($"High drawdown: {results.MaxDrawdownPercent:F1}% (above 30%, requires strong risk tolerance)");
            }

            // Rule 6: Average win/loss ratio
            if (results.AvgWinLossRatio < 0.8)
            {
                validation.AddWarning($"Low win/loss ratio: {results.AvgWinLossRatio:F2} (losses larger than wins, requires high win rate)");
            }

            // Rule 7: Perfect equity curve detection
            if (results.MaxConsecutiveLosses == 0 || results.MaxConsecutiveWins > 20)
            {
                validation.AddError("Suspiciously perfect results - likely overfit or look-ahead bias");
            }

            // Rule 8: Consecutive losses check
            if (results.MaxConsecutiveLosses > 10)
            {
                validation.AddWarning($"High consecutive losses: {results.MaxConsecutiveLosses} (psychological challenge)");
            }

            // Rule 9: Return vs drawdown ratio
            double returnDrawdownRatio = Math.Abs(results.ReturnPercent / results.MaxDrawdownPercent);
            if (returnDrawdownRatio < 1.0)
            {
                validation.AddWarning($"Return/Drawdown ratio: {returnDrawdownRatio:F2} (returns don't justify risk)");
            }

            // Rule 10: Transaction costs check
            if (!settings.IncludedCommissions || !settings.IncludedSpread)
            {
                validation.AddError("Backtest must include commissions and spread for realistic results");
            }

            // Rule 11: Data quality check
            if (!settings.UsedTickData)
            {
                validation.AddWarning("Backtest should use tick data for accuracy (not 1-minute bars)");
            }

            // Rule 12: Backtest duration
            if (results.BacktestDurationDays < 180)
            {
                validation.AddWarning($"Short backtest period: {results.BacktestDurationDays} days (minimum 6-12 months recommended)");
            }

            return validation;
        }

        /// <summary>
        /// Compares in-sample vs out-of-sample performance
        /// Critical for detecting overfitting
        /// </summary>
        public static OverfitAnalysis CheckForOverfitting(BacktestResults inSample, BacktestResults outOfSample)
        {
            var analysis = new OverfitAnalysis();

            // Compare Sharpe ratios
            double sharpeDifference = Math.Abs(inSample.SharpeRatio - outOfSample.SharpeRatio);
            double sharpeDecline = ((inSample.SharpeRatio - outOfSample.SharpeRatio) / inSample.SharpeRatio) * 100;

            if (sharpeDecline > 50)
            {
                analysis.AddWarning($"Sharpe ratio declined {sharpeDecline:F1}% (severe degradation, likely overfit)");
                analysis.OverfitProbability += 30;
            }
            else if (sharpeDecline > 30)
            {
                analysis.AddWarning($"Sharpe ratio declined {sharpeDecline:F1}% (moderate degradation)");
                analysis.OverfitProbability += 20;
            }

            // Compare win rates
            double winRateDifference = Math.Abs(inSample.WinRate - outOfSample.WinRate) * 100;
            if (winRateDifference > 15)
            {
                analysis.AddWarning($"Win rate changed by {winRateDifference:F1}% (significant inconsistency)");
                analysis.OverfitProbability += 20;
            }

            // Compare profit factors
            double profitFactorDecline = ((inSample.ProfitFactor - outOfSample.ProfitFactor) / inSample.ProfitFactor) * 100;
            if (profitFactorDecline > 40)
            {
                analysis.AddWarning($"Profit factor declined {profitFactorDecline:F1}% (severe degradation)");
                analysis.OverfitProbability += 25;
            }

            // Compare max drawdowns
            if (outOfSample.MaxDrawdownPercent > inSample.MaxDrawdownPercent * 1.5)
            {
                analysis.AddWarning($"Out-of-sample drawdown {outOfSample.MaxDrawdownPercent:F1}% much worse than in-sample {inSample.MaxDrawdownPercent:F1}%");
                analysis.OverfitProbability += 15;
            }

            // Compare returns
            double returnDecline = ((inSample.ReturnPercent - outOfSample.ReturnPercent) / inSample.ReturnPercent) * 100;
            if (outOfSample.ReturnPercent < 0 && inSample.ReturnPercent > 0)
            {
                analysis.AddWarning("Out-of-sample shows losses while in-sample was profitable (major red flag)");
                analysis.OverfitProbability += 40;
            }
            else if (returnDecline > 50)
            {
                analysis.AddWarning($"Return declined {returnDecline:F1}% (severe performance drop)");
                analysis.OverfitProbability += 25;
            }

            // Determine overall verdict
            if (analysis.OverfitProbability >= 70)
            {
                analysis.Verdict = "HIGHLY LIKELY OVERFIT - Do not use live";
            }
            else if (analysis.OverfitProbability >= 40)
            {
                analysis.Verdict = "POSSIBLE OVERFIT - Proceed with extreme caution";
            }
            else if (analysis.OverfitProbability >= 20)
            {
                analysis.Verdict = "ACCEPTABLE - Normal 10-30% degradation expected";
            }
            else
            {
                analysis.Verdict = "ROBUST - Out-of-sample performance consistent";
            }

            return analysis;
        }

        /// <summary>
        /// Calculate expected live performance range based on backtest degradation
        /// </summary>
        public static LivePerformanceExpectation EstimateLivePerformance(BacktestResults backtest)
        {
            // Assume 10-30% degradation from backtest to live (per guide)
            double conservativeDegradation = 0.30; // 30%
            double moderateDegradation = 0.20; // 20%
            double optimisticDegradation = 0.10; // 10%

            return new LivePerformanceExpectation
            {
                BacktestReturn = backtest.ReturnPercent,
                BacktestSharpe = backtest.SharpeRatio,
                BacktestDrawdown = backtest.MaxDrawdownPercent,

                ConservativeReturn = backtest.ReturnPercent * (1 - conservativeDegradation),
                ConservativeSharpe = backtest.SharpeRatio * (1 - conservativeDegradation),
                ConservativeDrawdown = backtest.MaxDrawdownPercent * (1 + conservativeDegradation),

                ModerateReturn = backtest.ReturnPercent * (1 - moderateDegradation),
                ModerateSharpe = backtest.SharpeRatio * (1 - moderateDegradation),
                ModerateDrawdown = backtest.MaxDrawdownPercent * (1 + moderateDegradation),

                OptimisticReturn = backtest.ReturnPercent * (1 - optimisticDegradation),
                OptimisticSharpe = backtest.SharpeRatio * (1 - optimisticDegradation),
                OptimisticDrawdown = backtest.MaxDrawdownPercent * (1 + optimisticDegradation)
            };
        }
    }

    #region Data Classes

    public class BacktestResults
    {
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public double WinRate { get; set; }
        public double ProfitFactor { get; set; }
        public double SharpeRatio { get; set; }
        public double MaxDrawdownPercent { get; set; }
        public double ReturnPercent { get; set; }
        public double AvgWinLossRatio { get; set; }
        public int MaxConsecutiveWins { get; set; }
        public int MaxConsecutiveLosses { get; set; }
        public int BacktestDurationDays { get; set; }
        public double TotalProfit { get; set; }
        public double AverageWin { get; set; }
        public double AverageLoss { get; set; }
    }

    public class BacktestSettings
    {
        public bool IncludedCommissions { get; set; }
        public bool IncludedSpread { get; set; }
        public bool UsedTickData { get; set; }
        public bool UsedVariableSpread { get; set; }
        public double SpreadMultiplier { get; set; } // 1.0 = normal, 2.0 = 2x for conservative testing
    }

    public class ValidationResult
    {
        public List<string> Errors { get; private set; }
        public List<string> Warnings { get; private set; }

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public bool IsValid => !HasErrors;

        public ValidationResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        public void AddError(string error)
        {
            Errors.Add($"[ERROR] {error}");
        }

        public void AddWarning(string warning)
        {
            Warnings.Add($"[WARNING] {warning}");
        }

        public string GetReport()
        {
            var report = "=== BACKTEST VALIDATION REPORT ===\n\n";

            if (HasErrors)
            {
                report += "ERRORS:\n";
                foreach (var error in Errors)
                {
                    report += $"  {error}\n";
                }
                report += "\n";
            }

            if (HasWarnings)
            {
                report += "WARNINGS:\n";
                foreach (var warning in Warnings)
                {
                    report += $"  {warning}\n";
                }
                report += "\n";
            }

            if (!HasErrors && !HasWarnings)
            {
                report += "✓ All validation checks passed!\n\n";
            }

            report += IsValid ? "VERDICT: PASSED\n" : "VERDICT: FAILED\n";
            report += "================================\n";

            return report;
        }
    }

    public class OverfitAnalysis
    {
        public List<string> Warnings { get; private set; }
        public double OverfitProbability { get; set; } // 0-100
        public string Verdict { get; set; }

        public OverfitAnalysis()
        {
            Warnings = new List<string>();
            OverfitProbability = 0;
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        public string GetReport()
        {
            var report = "=== OVERFITTING ANALYSIS ===\n\n";

            if (Warnings.Count > 0)
            {
                report += "Issues Detected:\n";
                foreach (var warning in Warnings)
                {
                    report += $"  • {warning}\n";
                }
                report += "\n";
            }

            report += $"Overfit Probability: {OverfitProbability:F0}%\n";
            report += $"Verdict: {Verdict}\n";
            report += "============================\n";

            return report;
        }
    }

    public class LivePerformanceExpectation
    {
        public double BacktestReturn { get; set; }
        public double BacktestSharpe { get; set; }
        public double BacktestDrawdown { get; set; }

        public double ConservativeReturn { get; set; }
        public double ConservativeSharpe { get; set; }
        public double ConservativeDrawdown { get; set; }

        public double ModerateReturn { get; set; }
        public double ModerateSharpe { get; set; }
        public double ModerateDrawdown { get; set; }

        public double OptimisticReturn { get; set; }
        public double OptimisticSharpe { get; set; }
        public double OptimisticDrawdown { get; set; }

        public string GetReport()
        {
            var report = "=== LIVE PERFORMANCE EXPECTATIONS ===\n\n";

            report += "Backtest Results:\n";
            report += $"  Return: {BacktestReturn:F1}%\n";
            report += $"  Sharpe: {BacktestSharpe:F2}\n";
            report += $"  Max DD: {BacktestDrawdown:F1}%\n\n";

            report += "Expected Live Performance (Conservative -30%):\n";
            report += $"  Return: {ConservativeReturn:F1}%\n";
            report += $"  Sharpe: {ConservativeSharpe:F2}\n";
            report += $"  Max DD: {ConservativeDrawdown:F1}%\n\n";

            report += "Expected Live Performance (Moderate -20%):\n";
            report += $"  Return: {ModerateReturn:F1}%\n";
            report += $"  Sharpe: {ModerateSharpe:F2}\n";
            report += $"  Max DD: {ModerateDrawdown:F1}%\n\n";

            report += "Expected Live Performance (Optimistic -10%):\n";
            report += $"  Return: {OptimisticReturn:F1}%\n";
            report += $"  Sharpe: {OptimisticSharpe:F2}\n";
            report += $"  Max DD: {OptimisticDrawdown:F1}%\n\n";

            report += "======================================\n";

            return report;
        }
    }

    #endregion
}

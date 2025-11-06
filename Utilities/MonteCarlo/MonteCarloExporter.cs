using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TradingBot.Utilities.MonteCarlo
{
    /// <summary>
    /// Exports trade data for Monte Carlo simulation in external tools
    /// Since cTrader has no native Monte Carlo, we export to:
    /// - Build Alpha Monte Carlo Simulator (free online)
    /// - Python scripts
    /// - Excel spreadsheets
    /// - R statistical packages
    /// </summary>
    public class MonteCarloExporter
    {
        /// <summary>
        /// Export trades to CSV format compatible with most MC simulators
        /// </summary>
        public static string ExportToCSV(List<TradeResult> trades)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("TradeNumber,ProfitLoss,Return%,Win");

            // Data rows
            int tradeNumber = 1;
            foreach (var trade in trades)
            {
                sb.AppendLine($"{tradeNumber},{trade.ProfitLoss:F2},{trade.ReturnPercent:F4},{(trade.IsWin ? 1 : 0)}");
                tradeNumber++;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export to format for Build Alpha MC Simulator
        /// URL: https://buildalpha.com/monte-carlo-simulator/
        /// </summary>
        public static string ExportForBuildAlpha(List<TradeResult> trades)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== BUILD ALPHA MONTE CARLO FORMAT ===");
            sb.AppendLine("Instructions: Copy the returns below and paste into Build Alpha simulator");
            sb.AppendLine("URL: https://buildalpha.com/monte-carlo-simulator/\n");

            sb.AppendLine("Returns (one per line):");

            foreach (var trade in trades)
            {
                sb.AppendLine(trade.ReturnPercent.ToString("F4"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export to Python format for custom Monte Carlo analysis
        /// </summary>
        public static string ExportToPython(List<TradeResult> trades, int simulations = 1000)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# Monte Carlo Simulation - Python Code");
            sb.AppendLine("# Run this with: python monte_carlo.py\n");

            sb.AppendLine("import numpy as np");
            sb.AppendLine("import matplotlib.pyplot as plt");
            sb.AppendLine("from scipy import stats\n");

            sb.AppendLine("# Trade results from backtest");
            sb.Append("trade_returns = [");
            sb.Append(string.Join(", ", trades.Select(t => t.ReturnPercent.ToString("F6"))));
            sb.AppendLine("]\n");

            sb.AppendLine($"# Monte Carlo simulation parameters");
            sb.AppendLine($"num_simulations = {simulations}");
            sb.AppendLine($"num_trades = len(trade_returns)\n");

            sb.AppendLine("# Run Monte Carlo simulation");
            sb.AppendLine("def monte_carlo_simulation(returns, num_sims, num_trades):");
            sb.AppendLine("    results = []");
            sb.AppendLine("    max_drawdowns = []");
            sb.AppendLine("    ");
            sb.AppendLine("    for _ in range(num_sims):");
            sb.AppendLine("        # Randomly resample trades with replacement");
            sb.AppendLine("        sampled_returns = np.random.choice(returns, size=num_trades, replace=True)");
            sb.AppendLine("        ");
            sb.AppendLine("        # Calculate equity curve");
            sb.AppendLine("        equity = [100]  # Starting capital");
            sb.AppendLine("        peak = 100");
            sb.AppendLine("        max_dd = 0");
            sb.AppendLine("        ");
            sb.AppendLine("        for ret in sampled_returns:");
            sb.AppendLine("            new_equity = equity[-1] * (1 + ret / 100)");
            sb.AppendLine("            equity.append(new_equity)");
            sb.AppendLine("            ");
            sb.AppendLine("            if new_equity > peak:");
            sb.AppendLine("                peak = new_equity");
            sb.AppendLine("            ");
            sb.AppendLine("            dd = ((peak - new_equity) / peak) * 100");
            sb.AppendLine("            if dd > max_dd:");
            sb.AppendLine("                max_dd = dd");
            sb.AppendLine("        ");
            sb.AppendLine("        final_return = ((equity[-1] - equity[0]) / equity[0]) * 100");
            sb.AppendLine("        results.append(final_return)");
            sb.AppendLine("        max_drawdowns.append(max_dd)");
            sb.AppendLine("    ");
            sb.AppendLine("    return results, max_drawdowns\n");

            sb.AppendLine("# Run simulation");
            sb.AppendLine("returns, drawdowns = monte_carlo_simulation(trade_returns, num_simulations, num_trades)\n");

            sb.AppendLine("# Calculate statistics");
            sb.AppendLine("print('=== MONTE CARLO RESULTS ===')\n");
            sb.AppendLine("print(f'Simulations: {num_simulations}')");
            sb.AppendLine("print(f'Trades per simulation: {num_trades}\\n')");

            sb.AppendLine("print('Return Distribution:')");
            sb.AppendLine("print(f'  Mean: {np.mean(returns):.2f}%')");
            sb.AppendLine("print(f'  Median: {np.median(returns):.2f}%')");
            sb.AppendLine("print(f'  Std Dev: {np.std(returns):.2f}%')");
            sb.AppendLine("print(f'  5th Percentile: {np.percentile(returns, 5):.2f}%')");
            sb.AppendLine("print(f'  95th Percentile: {np.percentile(returns, 95):.2f}%\\n')");

            sb.AppendLine("print('Drawdown Distribution:')");
            sb.AppendLine("print(f'  Mean Max DD: {np.mean(drawdowns):.2f}%')");
            sb.AppendLine("print(f'  Median Max DD: {np.median(drawdowns):.2f}%')");
            sb.AppendLine("print(f'  95th Percentile DD: {np.percentile(drawdowns, 95):.2f}%\\n')");

            sb.AppendLine("print('Risk of Ruin:')");
            sb.AppendLine("ruin_50 = (np.array(drawdowns) > 50).sum() / len(drawdowns) * 100");
            sb.AppendLine("ruin_30 = (np.array(drawdowns) > 30).sum() / len(drawdowns) * 100");
            sb.AppendLine("print(f'  Probability of 50%+ DD: {ruin_50:.2f}%')");
            sb.AppendLine("print(f'  Probability of 30%+ DD: {ruin_30:.2f}%\\n')");

            sb.AppendLine("# Plot results");
            sb.AppendLine("fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(14, 6))");
            sb.AppendLine("");
            sb.AppendLine("ax1.hist(returns, bins=50, edgecolor='black')");
            sb.AppendLine("ax1.axvline(np.mean(returns), color='r', linestyle='--', label=f'Mean: {np.mean(returns):.1f}%')");
            sb.AppendLine("ax1.set_xlabel('Return (%)')");
            sb.AppendLine("ax1.set_ylabel('Frequency')");
            sb.AppendLine("ax1.set_title('Monte Carlo Return Distribution')");
            sb.AppendLine("ax1.legend()");
            sb.AppendLine("ax1.grid(True, alpha=0.3)");
            sb.AppendLine("");
            sb.AppendLine("ax2.hist(drawdowns, bins=50, edgecolor='black')");
            sb.AppendLine("ax2.axvline(np.mean(drawdowns), color='r', linestyle='--', label=f'Mean: {np.mean(drawdowns):.1f}%')");
            sb.AppendLine("ax2.set_xlabel('Max Drawdown (%)')");
            sb.AppendLine("ax2.set_ylabel('Frequency')");
            sb.AppendLine("ax2.set_title('Monte Carlo Drawdown Distribution')");
            sb.AppendLine("ax2.legend()");
            sb.AppendLine("ax2.grid(True, alpha=0.3)");
            sb.AppendLine("");
            sb.AppendLine("plt.tight_layout()");
            sb.AppendLine("plt.savefig('monte_carlo_results.png', dpi=300, bbox_inches='tight')");
            sb.AppendLine("print('Plot saved as monte_carlo_results.png')");
            sb.AppendLine("plt.show()");

            return sb.ToString();
        }

        /// <summary>
        /// Generate Monte Carlo analysis instructions
        /// </summary>
        public static string GetMonteCarloInstructions()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== MONTE CARLO SIMULATION GUIDE ===\n");

            sb.AppendLine("WHY MONTE CARLO?");
            sb.AppendLine("Monte Carlo simulation helps you understand:");
            sb.AppendLine("  • Probability of different return outcomes");
            sb.AppendLine("  • Expected maximum drawdown (usually 1.5-2x backtest)");
            sb.AppendLine("  • Risk of ruin (account blow-up probability)");
            sb.AppendLine("  • Confidence intervals for strategy performance\n");

            sb.AppendLine("RECOMMENDED SIMULATION SETTINGS:");
            sb.AppendLine("  • Minimum 1,000 simulations (10,000 for production strategies)");
            sb.AppendLine("  • Use same number of trades as backtest");
            sb.AppendLine("  • Resample with replacement (bootstrap method)");
            sb.AppendLine("  • Run multiple simulations with different starting points\n");

            sb.AppendLine("KEY METRICS TO ANALYZE:");
            sb.AppendLine("  • 5th/95th percentile returns (confidence range)");
            sb.AppendLine("  • 95th percentile drawdown (expect this in live trading)");
            sb.AppendLine("  • Probability of 30%+ drawdown (acceptable if <10%)");
            sb.AppendLine("  • Probability of 50%+ drawdown (should be <2%)");
            sb.AppendLine("  • Risk of ruin calculation\n");

            sb.AppendLine("TOOLS FOR MONTE CARLO:");
            sb.AppendLine("  1. Build Alpha (Free Online): https://buildalpha.com/monte-carlo-simulator/");
            sb.AppendLine("     - Copy/paste returns directly");
            sb.AppendLine("     - Instant visualization");
            sb.AppendLine("     - No coding required\n");

            sb.AppendLine("  2. Python Script (Advanced):");
            sb.AppendLine("     - Full control over parameters");
            sb.AppendLine("     - Custom visualizations");
            sb.AppendLine("     - Statistical analysis");
            sb.AppendLine("     - Requires Python + numpy + matplotlib\n");

            sb.AppendLine("  3. Excel:");
            sb.AppendLine("     - Use RANDBETWEEN() to resample trades");
            sb.AppendLine("     - Create 1000+ columns for simulations");
            sb.AppendLine("     - Calculate percentiles with PERCENTILE()");
            sb.AppendLine("     - Good for simple analysis\n");

            sb.AppendLine("  4. R Statistical Package:");
            sb.AppendLine("     - Use sample() with replace=TRUE");
            sb.AppendLine("     - PerformanceAnalytics package");
            sb.AppendLine("     - Comprehensive risk metrics\n");

            sb.AppendLine("INTERPRETATION GUIDE:");
            sb.AppendLine("  • 95th percentile DD < 30%: Good");
            sb.AppendLine("  • 95th percentile DD 30-50%: Moderate risk");
            sb.AppendLine("  • 95th percentile DD > 50%: High risk\n");

            sb.AppendLine("  • Probability of 50%+ DD < 2%: Acceptable");
            sb.AppendLine("  • Probability of 50%+ DD 2-5%: Caution");
            sb.AppendLine("  • Probability of 50%+ DD > 5%: Too risky\n");

            sb.AppendLine("EXAMPLE WORKFLOW:");
            sb.AppendLine("  1. Run cTrader backtest and export trades");
            sb.AppendLine("  2. Use MonteCarloExporter to generate Python script");
            sb.AppendLine("  3. Run Python script: python monte_carlo.py");
            sb.AppendLine("  4. Analyze statistics and visualizations");
            sb.AppendLine("  5. If 95th percentile DD acceptable, proceed to demo");
            sb.AppendLine("  6. If risk of ruin too high, reduce position sizing\n");

            sb.AppendLine("======================================\n");

            return sb.ToString();
        }

        /// <summary>
        /// Calculate risk of ruin based on win rate and risk-reward
        /// </summary>
        public static RiskOfRuinAnalysis CalculateRiskOfRuin(double winRate, double avgWin, double avgLoss, double riskPerTrade)
        {
            // Risk of Ruin formula (simplified)
            // ROR = ((1 - W) / W) ^ (Capital / RiskPerTrade)
            // where W = win probability adjusted for win/loss size

            double lossRate = 1 - winRate;
            double winLossRatio = avgWin / Math.Abs(avgLoss);

            // Calculate probability adjustment
            double adjustedWinProb = (winRate * winLossRatio) / (winRate * winLossRatio + lossRate);

            var analysis = new RiskOfRuinAnalysis
            {
                WinRate = winRate,
                AvgWin = avgWin,
                AvgLoss = avgLoss,
                RiskPerTrade = riskPerTrade,
                WinLossRatio = winLossRatio
            };

            // Calculate ROR for different capital levels
            for (int capital = 10; capital <= 100; capital += 10)
            {
                double units = capital / riskPerTrade;
                double ror = Math.Pow((lossRate / winRate), units);

                analysis.RiskOfRuinByCapital[capital] = Math.Min(ror * 100, 100); // Cap at 100%
            }

            return analysis;
        }
    }

    #region Data Classes

    public class TradeResult
    {
        public double ProfitLoss { get; set; }
        public double ReturnPercent { get; set; }
        public bool IsWin => ProfitLoss > 0;
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
    }

    public class RiskOfRuinAnalysis
    {
        public double WinRate { get; set; }
        public double AvgWin { get; set; }
        public double AvgLoss { get; set; }
        public double RiskPerTrade { get; set; }
        public double WinLossRatio { get; set; }
        public Dictionary<int, double> RiskOfRuinByCapital { get; set; }

        public RiskOfRuinAnalysis()
        {
            RiskOfRuinByCapital = new Dictionary<int, double>();
        }

        public string GetReport()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== RISK OF RUIN ANALYSIS ===\n");

            sb.AppendLine("Strategy Parameters:");
            sb.AppendLine($"  Win Rate: {WinRate * 100:F1}%");
            sb.AppendLine($"  Avg Win: {AvgWin:F2}");
            sb.AppendLine($"  Avg Loss: {AvgLoss:F2}");
            sb.AppendLine($"  Win/Loss Ratio: {WinLossRatio:F2}");
            sb.AppendLine($"  Risk Per Trade: {RiskPerTrade:F1}%\n");

            sb.AppendLine("Risk of Ruin by Capital (as % of risk per trade):");

            foreach (var kvp in RiskOfRuinByCapital.OrderBy(x => x.Key))
            {
                string risk = kvp.Value < 0.01 ? "<0.01%" :
                             kvp.Value < 1 ? $"{kvp.Value:F2}%" :
                             kvp.Value < 10 ? $"{kvp.Value:F1}%" :
                             $"{kvp.Value:F0}%";

                string assessment = kvp.Value < 1 ? "EXCELLENT" :
                                  kvp.Value < 5 ? "GOOD" :
                                  kvp.Value < 10 ? "ACCEPTABLE" :
                                  kvp.Value < 20 ? "HIGH RISK" :
                                  "VERY HIGH RISK";

                sb.AppendLine($"  {kvp.Key}x Capital: {risk} [{assessment}]");
            }

            sb.AppendLine("\nRecommendation:");
            if (RiskOfRuinByCapital.ContainsKey(50))
            {
                double ror50 = RiskOfRuinByCapital[50];
                if (ror50 < 1)
                {
                    sb.AppendLine($"  Excellent - Very low risk of ruin ({ror50:F2}% at 50x capital)");
                }
                else if (ror50 < 5)
                {
                    sb.AppendLine($"  Good - Acceptable risk of ruin ({ror50:F1}% at 50x capital)");
                }
                else if (ror50 < 10)
                {
                    sb.AppendLine($"  Moderate - Consider reducing risk per trade ({ror50:F1}% at 50x capital)");
                }
                else
                {
                    sb.AppendLine($"  High Risk - Reduce risk per trade significantly ({ror50:F0}% at 50x capital)");
                }
            }

            sb.AppendLine("\n============================\n");

            return sb.ToString();
        }
    }

    #endregion
}

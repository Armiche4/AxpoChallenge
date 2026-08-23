using Axpo;
using PowerPositionReport.Aggregation;

namespace PowerPositionReport.Test;

public class PowerPositionAggregatorTests
{
    // Replicates exactly the challenge README's example: two trades for 4/1/2015 and the
    // hourly volume table the README gives as the expected result.
    [Fact]
    public void Aggregate_ReplicatesReadmeExample()
    {
        var trade1 = PowerTrade.Create(new DateTime(2015, 1, 4), 24);
        for (var i = 0; i < trade1.Periods.Length; i++)
        {
            trade1.Periods[i].SetVolume(100);
        }

        var trade2 = PowerTrade.Create(new DateTime(2015, 1, 4), 24);
        for (var i = 0; i < trade2.Periods.Length; i++)
        {
            // Periods 1-11 (indices 0-10) -> 50; periods 12-24 (indices 11-23) -> -20.
            trade2.Periods[i].SetVolume(i < 11 ? 50 : -20);
        }

        var aggregator = new PowerPositionAggregator();
        var trades = new List<PowerTrade> { trade1, trade2 };

        var result = aggregator.Aggregate(trades);

        // (hour, expected volume) exactly as the README's table, in order: 23:00 -> 22:00.
        var expected = new (int Hour, double Volume)[]
        {
            (23, 150), (0, 150), (1, 150), (2, 150), (3, 150), (4, 150),
            (5, 150), (6, 150), (7, 150), (8, 150), (9, 150),
            (10, 80), (11, 80), (12, 80), (13, 80), (14, 80), (15, 80),
            (16, 80), (17, 80), (18, 80), (19, 80), (20, 80), (21, 80), (22, 80),
        };

        Assert.Equal(expected.Length, result.Count);

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Hour, result[i].LocalHour.Hour);
            Assert.Equal(0, result[i].LocalHour.Minute);
            Assert.Equal(expected[i].Volume, result[i].Volume);
        }
    }

    // The two clock-change days. PowerService derives its period count from the real length of
    // the local day, so it returns 23 or 25 periods instead of 24: the report must follow.
    [Fact]
    public void Aggregate_SkipsTheHourThatNeverHappens_WhenTheClocksGoForward()
    {
        // 29 Mar 2026: the local day lasts 23 hours because 01:00 does not exist. There must be
        // no row for it, and none invented with volume 0 either. Period 3 lands straight on 02:00.
        var trade = TradeWithVolumeEqualToPeriodNumber(new DateTime(2026, 3, 29), 23);

        var result = new PowerPositionAggregator().Aggregate(new[] { trade });

        Assert.Equal(23, result.Count);
        Assert.DoesNotContain(result, hourlyVolume => hourlyVolume.LocalHour.Hour == 1);
        Assert.Equal(new TimeOnly(2, 0), result[2].LocalHour);//In a normal day this would be 1:00, but the clock jumped forward to 2:00.
    }

    [Fact]
    public void Aggregate_KeepsBothDeliveryHoursApart_WhenTheClocksGoBack()
    {
        // 25 Oct 2026: the local day lasts 25 hours because 01:00 happens twice. Periods 3 and 4
        // are two different delivery hours sharing that label, so they must stay two rows.
        // Grouping by the "HH:mm" label instead of by the instant would merge them into one.
        var trade = TradeWithVolumeEqualToPeriodNumber(new DateTime(2026, 10, 25), 25);

        var result = new PowerPositionAggregator().Aggregate(new[] { trade });

        Assert.Equal(25, result.Count);
        Assert.Equal(new TimeOnly(1, 0), result[2].LocalHour);//In a normal day this would be 1:00, but the clock jumped back to 1:00 again.
        Assert.Equal(new TimeOnly(1, 0), result[3].LocalHour);
    }

    // Volume = period number, so every row can be traced back to the period it came from.
    private static PowerTrade TradeWithVolumeEqualToPeriodNumber(DateTime date, int numberOfPeriods)
    {
        var trade = PowerTrade.Create(date, numberOfPeriods);
        for (var i = 0; i < trade.Periods.Length; i++)
        {
            trade.Periods[i].SetVolume(i + 1);
        }

        return trade;
    }
}

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
}

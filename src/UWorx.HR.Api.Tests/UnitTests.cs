namespace UWorx.HR.Api.Tests;

internal class UnitTests
{
    (string, string) getWeather() => ("Faisalabad", "Hot");

    int add(int x, int y) => x + y;

    [SetUp]
    public void Setup()
    { }

    [Test]
    public void GetWeatherTest()
    {
        string city, weather;
        (city, weather) = getWeather();
        Assert.IsTrue(city == "Lahore" && weather == "Hot");
    }

    [Test]
    public void GetWeatherTestMultipleAsserts()
    {
        string city, weather;
        (city, weather) = getWeather();

        Assert.Multiple(() =>
        {
            Assert.That(city, Is.EqualTo("Lahore"));
            Assert.That(weather, Is.EqualTo("Hot"));
        });
    }

    [TestCase(1, 1, 2)]
    [TestCase(2, 2, 4)]
    [TestCase(-1, 2, 1)]
    public void AddTests(int x, int y, int result)
    {
        var r = add(x, y);

        Assert.IsTrue(r == result);
    }
}

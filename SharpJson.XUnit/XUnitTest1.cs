using Globals;
//using static Globals.SharpJson;
using Xunit;
using Xunit.Abstractions;

public class XUnitTest1
{
    private readonly ITestOutputHelper Out;
    public XUnitTest1(ITestOutputHelper testOutputHelper)
    {
        Out = testOutputHelper;
        SharpJson.ClearAllSettings();
        Print("Setup() called");
    }
    private void Print(object x, string title = null)
    {
        Out.WriteLine(SharpJson.ToPrintable(x, title));
    }
    [Fact]
    public void Test01()
    {
        SharpJson.SetShowDetail(true);
        dynamic dyn = SharpJson.FromObject(12345.67);
        string json = SharpJson.ToJson(dyn);
        Print(json, "json");
        Assert.Equal("12345.67", json);
    }
    [Fact]
    public void Test02()
    {
        SharpJson.SetShowDetail(true);
        var o = SharpJson.FromObject(new { a = 123 });
        Print(o, "o");
        Assert.False(o.IsArray);
        Assert.True(o.IsObject);
        Assert.Equal("""
            <Globals.SharpJson> {
              "a": 123
            }
            """.Replace("\r\n", "\n"), SharpJson.ToPrintable(o));
    }
}

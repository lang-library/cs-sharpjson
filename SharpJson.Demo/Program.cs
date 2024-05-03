using System;
using System.Dynamic;
using Globals;
using Xunit;
using static Globals.SharpJson;
namespace Main;


class MyClass
{
    protected int abc = 123;
    public int ABC {
        get { return abc; }
        set { abc = value; }
    }
    public static int Add2(int x, int y)
    {
        return x + y;
    }
}
static class Program
{
    [STAThread]
    static void Main(string[] originalArgs)
    {
        ShowDetail = true;
        // Parse (from JsonString to DynamicJson)
        var dyn = SharpJson.FromJson(@"{""foo"":""json"", ""bar"":100, ""nest"":{ ""foobar"":true }, ""ary"": [11, 22, 33] }");
        string built = JsonBuilder.BuildFromObject(dyn);
        Echo(built);
        //Environment.Exit(0);
        Echo(dyn, "json");
        var r1 = dyn.foo; // "json" - dynamic(string)
        Echo($"r1={r1}");
        var r2 = dyn.bar; // 100 - dynamic(double)
        Echo($"r2={r2}");
        var r3 = dyn.nest.foobar; // true - dynamic(bool)
        Echo($"r3={r3}");
        var r4 = dyn["nest"]["foobar"]; // can access indexer
        Echo($"r4={r4}");
        var json2 = SharpJson.FromJson(@"[11, 22, 33]");
        Echo(json2);
        var array2 = (int[])json2;
        Echo(array2.Length);
        dynamic exp = new ExpandoObject();
        exp.A_ = 123;
        exp.B_ = "abc";
        Echo(exp, "exp");
        string s1 = SharpJson.FromObject(exp).ToJson();
        Echo(s1);
        string s2 = SharpJson.FromObject(new { A = 123 }).ToJson();
        Echo(s2);
        var cls = new MyClass();
        Echo(cls, "cls");
        string s3 = SharpJson.FromObject(cls).ToJson();
        Echo(s3 is string);
        Echo(s3);
        var d = SharpJson.FromObject(cls);
        Echo(d, "d");
        Echo(d.GetJsonType());
        Echo(d.GetJsonType().ToString());
        Echo(d.GetJsonType() == JsonType.@object);
        var cls2 = SharpJson.FromJson(@"{ ""ABC"":777}").ToObject<MyClass>();
        Echo(cls2, "cls2");
        SharpJson.SetShowDetail(true);
        //var dyn2 = SharpJson.FromObject(12345.67);
        var dyn2 = SharpJson.FromObject(12345678901234567890123456789m);
        Echo(FullName(dyn2));
        string json = ToJson(dyn2);
        Echo(json, "json");
        Assert.Equal("12345678901234567890123456789", json);
        var dyn3 = SharpJson.FromJson("[11, 22, 33]");
        Echo(dyn3.Count, "dyn3.Count");
        foreach(var e3 in dyn3)
        {
            Echo(e3, "e3");
        }
        var dyn4 = SharpJson.FromJson(@"{ ""a"": 111, ""b"": 222 }");
        Echo(dyn4.Keys);
        foreach (var e4 in dyn4.Keys)
        {
            Echo(dyn4[e4]);
        }
        foreach (var e4 in dyn4)
        {
            Echo(e4.Key);
            Echo(e4.Value);
        }
    }
}

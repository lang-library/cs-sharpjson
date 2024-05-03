#region License and information
/*
------------------------------------------------------------------------------
This software is available under 2 licenses -- choose whichever you prefer.
------------------------------------------------------------------------------
ALTERNATIVE A - MIT License
Copyright (c) 2024 JavaCommons Technologies
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
------------------------------------------------------------------------------
ALTERNATIVE B - Public Domain (www.unlicense.org)
This is free and unencumbered software released into the public domain.
Anyone is free to copy, modify, publish, use, compile, sell, or distribute this
software, either in source code form or as a compiled binary, for any purpose,
commercial or non-commercial, and by any means.
In jurisdictions that recognize copyright laws, the author or authors of this
software dedicate any and all copyright interest in the software to the public
domain. We make this dedication for the benefit of the public at large and to
the detriment of our heirs and successors. We intend this dedication to be an
overt act of relinquishment in perpetuity of all present and future rights to
this software under copyright law.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
-----------------------------------------------------------------------------
*/
#endregion License and information

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Globals;

public enum JsonType
{
    @string, @number, @boolean, @object, @array, @null
}

public class SharpJson : DynamicObject
{
    #region flags
    public static bool DebugOutput = false;
    public static bool ShowDetail = false;
    public static MyDataFlagSetter ClearAllSettings()
    {
        return
            SetDebugOutput(false)
            .SetShowDetail(false)
            ;
    }
    public static MyDataFlagSetter SetDebugOutput(bool flag)
    {
        return new MyDataFlagSetter().SetDebugOutput(flag);
    }
    public static MyDataFlagSetter SetShowDetail(bool flag)
    {
        return new MyDataFlagSetter().SetShowDetail(flag);
    }
    public class MyDataFlagSetter
    {
        public MyDataFlagSetter SetDebugOutput(bool flag)
        {
            DebugOutput = flag;
            return this;
        }
        public MyDataFlagSetter SetShowDetail(bool flag)
        {
            ShowDetail = flag;
            return this;
        }
    }
    #endregion flags

    // public static methods

    /// <summary>from JsonSring to DynamicJson</summary>
    public static dynamic FromJson(string json)
    {
        return FromJson(json, Encoding.Unicode);
    }

    /// <summary>from JsonSring to DynamicJson</summary>
    public static dynamic FromJson(string json, Encoding encoding)
    {
        using (var reader = JsonReaderWriterFactory.CreateJsonReader(encoding.GetBytes(json), XmlDictionaryReaderQuotas.Max))
        {
            return ToValue(XElement.Load(reader));
        }
    }

    /// <summary>from JsonSringStream to DynamicJson</summary>
    public static dynamic FromJson(Stream stream)
    {
        using (var reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max))
        {
            return ToValue(XElement.Load(reader));
        }
    }

    /// <summary>from JsonSringStream to DynamicJson</summary>
    public static dynamic FromJson(Stream stream, Encoding encoding)
    {
        using (var reader = JsonReaderWriterFactory.CreateJsonReader(stream, encoding, XmlDictionaryReaderQuotas.Max, _ => { }))
        {
            return ToValue(XElement.Load(reader));
        }
    }

    /// <summary>create JsonSring from primitive or IEnumerable or Object({public property name:property value})</summary>
    public static string ToJson(object obj, bool indent = false)
    {
        if (obj is SharpJson) return (obj as SharpJson).ToJson(indent);
        //return CreateJsonString(new XStreamingElement("root", CreateTypeAttr(GetJsonType(obj)), CreateJsonNode(obj)), indent).Replace("\r\n", "\n");
        string json = JsonBuilder.BuildFromObject(obj);
        obj = SharpJson.FromJson(json);
        if (obj is SharpJson) return (obj as SharpJson).ToJson(indent);
        return json;
    }

    public int Count
    {
        get
        {
            return (IsArray) ? xml.Elements().Count() : 0;
        }
    }

    public List<string> Keys
    {
        get
        {
            List<string> keys = new List<string>();
            if (IsArray) return keys;
            foreach(var elem in xml.Elements())
            {
                keys.Add(elem.Name.LocalName);
            }
            return keys;
        }
    }

    // private static methods

    private static dynamic ToValue(XElement element)
    {
        var type = (JsonType)Enum.Parse(typeof(JsonType), element.Attribute("type").Value);
        switch (type)
        {
            case JsonType.@boolean:
                return (bool)element;
            case JsonType.@number:
                //return (double)element;
                return (decimal)element;
            case JsonType.@string:
                return (string)element;
            case JsonType.@object:
            case JsonType.@array:
                return new SharpJson(element, type);
            case JsonType.@null:
            default:
                return null;
        }
    }

#if false
    public JsonType GetJsonType()
    {
        return GetJsonType(this);
    }
#endif

    public static JsonType GetJsonType(object obj)
    {
        if (obj == null) return JsonType.@null;

        switch (Type.GetTypeCode(obj.GetType()))
        {
            case TypeCode.Boolean:
                return JsonType.@boolean;
            case TypeCode.String:
            case TypeCode.Char:
            case TypeCode.DateTime:
                return JsonType.@string;
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
            case TypeCode.SByte:
            case TypeCode.Byte:
                return JsonType.@number;
            case TypeCode.Object:
                return (obj is IEnumerable) ? JsonType.@array : JsonType.@object;
            case TypeCode.DBNull:
            case TypeCode.Empty:
            default:
                return JsonType.@null;
        }
    }

    private static XAttribute CreateTypeAttr(JsonType type)
    {
        return new XAttribute("type", type.ToString());
    }

    private static object CreateJsonNode(object obj)
    {
        var type = GetJsonType(obj);
        switch (type)
        {
            case JsonType.@string:
            case JsonType.number:
                return obj;
            case JsonType.@boolean:
                return obj.ToString().ToLower();
            case JsonType.@object:
                return CreateXObject(obj);
            case JsonType.@array:
                return CreateXArray(obj as IEnumerable);
            case JsonType.@null:
            default:
                return null;
        }
    }

    private static IEnumerable<XStreamingElement> CreateXArray<T>(T obj) where T : IEnumerable
    {
        return obj.Cast<object>()
            .Select(o => new XStreamingElement("item", CreateTypeAttr(GetJsonType(o)), CreateJsonNode(o)));
    }

    private static IEnumerable<XStreamingElement> CreateXObject(object obj)
    {
        return obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(pi => new { Name = pi.Name, Value = pi.GetValue(obj, null) })
            .Select(a => new XStreamingElement(a.Name, CreateTypeAttr(GetJsonType(a.Value)), CreateJsonNode(a.Value)));
    }

    private static string CreateJsonString(XStreamingElement element, bool indent)
    {
        using (var ms = new MemoryStream())
#if false
        using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.Unicode))
#else
        using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.Unicode, true, indent))
#endif
        {
            element.WriteTo(writer);
            writer.Flush();
            return Encoding.Unicode.GetString(ms.ToArray());
        }
    }

    // dynamic structure represents JavaScript Object/Array

    readonly XElement xml;
    readonly JsonType jsonType;

    /// <summary>create blank JSObject</summary>
    public SharpJson()
    {
        xml = new XElement("root", CreateTypeAttr(JsonType.@object));
        jsonType = JsonType.@object;
    }

    private SharpJson(XElement element, JsonType type)
    {
        System.Diagnostics.Debug.Assert(type == JsonType.array || type == JsonType.@object);

        xml = element;
        jsonType = type;
    }

    public bool IsObject { get { return jsonType == JsonType.@object; } }

    public bool IsArray { get { return jsonType == JsonType.@array; } }

    /// <summary>has property or not</summary>
    public bool IsDefined(string name)
    {
        return IsObject && (xml.Element(name) != null);
    }

    /// <summary>has property or not</summary>
    public bool IsDefined(int index)
    {
        return IsArray && (xml.Elements().ElementAtOrDefault(index) != null);
    }

    /// <summary>delete property</summary>
    public bool Delete(string name)
    {
        var elem = xml.Element(name);
        if (elem != null)
        {
            elem.Remove();
            return true;
        }
        else return false;
    }

    /// <summary>delete property</summary>
    public bool Delete(int index)
    {
        var elem = xml.Elements().ElementAtOrDefault(index);
        if (elem != null)
        {
            elem.Remove();
            return true;
        }
        else return false;
    }

    /// <summary>mapping to Array or Class by Public PropertyName</summary>
    public T ToObject<T>()
    {
        return (T)ToObject(typeof(T));
    }

    private object ToObject(Type type)
    {
        return (IsArray) ? DeserializeArray(type) : DeserializeObject(type);
    }

    private dynamic DeserializeValue(XElement element, Type elementType)
    {
        var value = ToValue(element);
        if (value is SharpJson)
        {
            value = ((SharpJson)value).ToObject(elementType);
        }
        return Convert.ChangeType(value, elementType);
    }

    private object DeserializeObject(Type targetType)
    {
        var result = Activator.CreateInstance(targetType);
        var dict = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(pi => pi.Name, pi => pi);
        foreach (var item in xml.Elements())
        {
            PropertyInfo propertyInfo;
            if (!dict.TryGetValue(item.Name.LocalName, out propertyInfo)) continue;
            var value = DeserializeValue(item, propertyInfo.PropertyType);
            propertyInfo.SetValue(result, value, null);
        }
        return result;
    }

    private object DeserializeArray(Type targetType)
    {
        if (targetType.IsArray) // Foo[]
        {
            var elemType = targetType.GetElementType();
            dynamic array = Array.CreateInstance(elemType, xml.Elements().Count());
            var index = 0;
            foreach (var item in xml.Elements())
            {
                array[index++] = DeserializeValue(item, elemType);
            }
            return array;
        }
        else // List<Foo>
        {
            var elemType = targetType.GetGenericArguments()[0];
            dynamic list = Activator.CreateInstance(targetType);
            foreach (var item in xml.Elements())
            {
                list.Add(DeserializeValue(item, elemType));
            }
            return list;
        }
    }

    // Delete
    public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
    {
        result = (IsArray)
            ? Delete((int)args[0])
            : Delete((string)args[0]);
        return true;
    }

    // IsDefined, if has args then TryGetMember
    public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
    {
        if (args.Length > 0)
        {
            result = null;
            return false;
        }

        result = IsDefined(binder.Name);
        return true;
    }

    // Deserialize or foreach(IEnumerable)
    public override bool TryConvert(ConvertBinder binder, out object result)
    {
        if (binder.Type == typeof(IEnumerable) || binder.Type == typeof(object[]))
        {
            var ie = (IsArray)
                ? xml.Elements().Select(x => ToValue(x))
                : xml.Elements().Select(x => (dynamic)new KeyValuePair<string, object>(x.Name.LocalName, ToValue(x)));
            result = (binder.Type == typeof(object[])) ? ie.ToArray() : ie;
        }
        else
        {
            result = ToObject(binder.Type);
        }
        return true;
    }

    private bool TryGet(XElement element, out object result)
    {
        if (element == null)
        {
            result = null;
            return false;
        }

        result = ToValue(element);
        return true;
    }

    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
    {
        return (IsArray)
            ? TryGet(xml.Elements().ElementAtOrDefault((int)indexes[0]), out result)
            : TryGet(xml.Element((string)indexes[0]), out result);
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        return (IsArray)
            ? TryGet(xml.Elements().ElementAtOrDefault(int.Parse(binder.Name)), out result)
            : TryGet(xml.Element(binder.Name), out result);
    }

    private bool TrySet(string name, object value)
    {
        var type = GetJsonType(value);
        var element = xml.Element(name);
        if (element == null)
        {
            xml.Add(new XElement(name, CreateTypeAttr(type), CreateJsonNode(value)));
        }
        else
        {
            element.Attribute("type").Value = type.ToString();
            element.ReplaceNodes(CreateJsonNode(value));
        }

        return true;
    }

    private bool TrySet(int index, object value)
    {
        var type = GetJsonType(value);
        var e = xml.Elements().ElementAtOrDefault(index);
        if (e == null)
        {
            xml.Add(new XElement("item", CreateTypeAttr(type), CreateJsonNode(value)));
        }
        else
        {
            e.Attribute("type").Value = type.ToString();
            e.ReplaceNodes(CreateJsonNode(value));
        }

        return true;
    }

    public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
    {
        return (IsArray)
            ? TrySet((int)indexes[0], value)
            : TrySet((string)indexes[0], value);
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        return (IsArray)
            ? TrySet(int.Parse(binder.Name), value)
            : TrySet(binder.Name, value);
    }

    public override IEnumerable<string> GetDynamicMemberNames()
    {
        return (IsArray)
            ? xml.Elements().Select((x, i) => i.ToString())
            : xml.Elements().Select(x => x.Name.LocalName);
    }

    /// <summary>Serialize to JsonString</summary>
    public override string ToString()
    {
        return this.ToJson(true);
    }

    public string ToJson(bool indent = false)
    {
        // <foo type="null"></foo> is can't serialize. replace to <foo type="null" />
        foreach (var elem in xml.Descendants().Where(x => x.Attribute("type").Value == "null"))
        {
            elem.RemoveNodes();
        }
        return CreateJsonString(new XStreamingElement("root", CreateTypeAttr(jsonType), xml.Elements()), indent).Replace("\r\n", "\n");
    }

    public static void Echo(object x, string title = null)
    {
        String s = ToPrintable(x, title);
        Console.WriteLine(s);
        System.Diagnostics.Debug.WriteLine(s);
    }
    public static void Log(dynamic x, string? title = null)
    {
        String s = ToPrintable(x, title);
        Console.Error.WriteLine("[Log] " + s);
        System.Diagnostics.Debug.WriteLine("[Log] " + s);
    }
    public static void Debug(dynamic x, string? title = null)
    {
        if (!DebugOutput) return;
        String s = ToPrintable(x, title);
        Console.Error.WriteLine("[Debug] " + s);
        System.Diagnostics.Debug.WriteLine("[Debug] " + s);
    }
    public static void Message(dynamic x, string? title = null)
    {
        if (title == null) title = "Message";
        String s = ToPrintable(x);
        NativeMethods.MessageBoxW(IntPtr.Zero, s, title, 0);
    }
    internal static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int MessageBoxW(
            IntPtr hWnd, string lpText, string lpCaption, uint uType);
    }

    public static dynamic FromObject(dynamic d)
    {
        string json = ToJson(d);
        return FromJson(json);
    }
    public static string ToPrintable(object x, string title = null)
    {
        string s = "";
        if (title != null) s = title + ": ";
        if (x is null) return s + "null";
        if (x is string)
        {
            if (!ShowDetail) return s + (string)x;
            return s + "`" + (string)x + "`";
        }
        string output = null;
        try
        {
            output = ToJson(x, true);
        }
        catch (Exception)
        {
            output = x.ToString();
        }
        if (!ShowDetail) return s + output;
        return s + $"<{FullName(x)}> {output}";
    }
    public static string FullName(dynamic x)
    {
        if (x is null) return "null";
        string fullName = ((object)x).GetType().FullName;
        return fullName.Split('`')[0];
    }
}

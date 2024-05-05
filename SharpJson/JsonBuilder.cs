using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;

namespace Global;

public class JsonBuilder
{
    public static string BuildFromObject(object x)
    {
        StringBuilder sb = new StringBuilder();
        WriteToSB(sb, x);
        string json = sb.ToString();
        //Console.WriteLine(json);
        return json;
    }
    static void WriteToSB(StringBuilder sb, object item)
    {
        if (item == null)
        {
            sb.Append("null");
            return;
        }

        Type type = item.GetType();
        if (type == typeof(SharpJson))
        {
            sb.Append(((SharpJson)item).ToJson());
            return;
        }
        if (type == typeof(string) || type == typeof(char))
        {
            string str = item.ToString();
            sb.Append('"');
            sb.Append(Escape(str));
            sb.Append('"');
            return;
        }
        else if (type == typeof(byte) || type == typeof(sbyte))
        {
            sb.Append(item.ToString());
            return;
        }
        else if (type == typeof(short) || type == typeof(ushort))
        {
            sb.Append(item.ToString());
            return;
        }
        else if (type == typeof(int) || type == typeof(uint))
        {
            sb.Append(item.ToString());
            return;
        }
        else if (type == typeof(long) || type == typeof(ulong))
        {
            sb.Append(item.ToString());
            return;
        }
        else if (type == typeof(float))
        {
            sb.Append(item.ToString());
            return;
        }
        else if (type == typeof(double))
        {
            sb.Append(item.ToString());
            return;
        }
        else if (type == typeof(decimal))
        {
            sb.Append(item.ToString());
            return;
        }
        else if (type == typeof(bool))
        {
            sb.Append(item.ToString().ToLower());
            return;
        }
        else if (type == typeof(DateTime))
        {
            WriteToSB(sb, ((DateTime)item).ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"));
            return;
        }
        else if (type == typeof(TimeSpan))
        {
            WriteToSB(sb, item.ToString());
            return;
        }
        else if (type == typeof(Guid))
        {
            WriteToSB(sb, item.ToString());
            return;
        }
        else if (type.IsEnum)
        {
            WriteToSB(sb, item.ToString());
            return;
        }
        else if (item is ExpandoObject)
        {
            var dic = item as IDictionary<string, object>;
            var result = new Dictionary<string, object>();
            foreach (var key in dic.Keys)
            {
                result[key] = dic[key];
            }
            WriteToSB(sb, result);
            return;
        }
        else if (item is IList)
        {
            IList list = item as IList;
            sb.Append('[');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(",");
                WriteToSB(sb, list[i]);
            }
            sb.Append(']');
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            Type keyType = type.GetGenericArguments()[0];
            //Refuse to output dictionary keys that aren't of type string
            if (keyType != typeof(string))
            {
                sb.Append("{}");
                return;
            }
            IDictionary dict = item as IDictionary;
            sb.Append("{");
            int count = 0;
            foreach (object key in dict.Keys)
            {
                if (count > 0) sb.Append(',');
                WriteToSB(sb, (string)key);
                sb.Append(':');
                WriteToSB(sb, dict[key]);
                count++;
            }
            sb.Append("}");
            return;
        }
        else
        {
            int count = 0;
            sb.Append('{');
#if false
            var gArgs = type.GetGenericArguments();
            if (gArgs.Length == 0)
            {
                sb.Append('}');
                return;
            }
            //Type keyType = gArgs[0];
#endif
            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            for (int i = 0; i < fieldInfos.Length; i++)
            {
                if (count > 0) sb.Append(',');
                if (fieldInfos[i].IsDefined(typeof(IgnoreDataMemberAttribute), true))
                    continue;
                object value = fieldInfos[i].GetValue(item);
                WriteToSB(sb, GetMemberName(fieldInfos[i]));
                sb.Append(':');
                WriteToSB(sb, value);
                count++;
            }
            PropertyInfo[] propertyInfo = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            for (int i = 0; i < propertyInfo.Length; i++)
            {
                if (count > 0) sb.Append(',');
                if (!propertyInfo[i].CanRead || propertyInfo[i].IsDefined(typeof(IgnoreDataMemberAttribute), true))
                    continue;
                object value = propertyInfo[i].GetValue(item, null);
                WriteToSB(sb, GetMemberName(propertyInfo[i]));
                sb.Append(':');
                WriteToSB(sb,value);
                count++;
            }
            sb.Append('}');
            return;
        }
    }
    static string Escape(string aText, bool forceAscii = false)
    {
        var sb = new StringBuilder();
        sb.Length = 0;
        if (sb.Capacity < aText.Length + aText.Length / 10)
            sb.Capacity = aText.Length + aText.Length / 10;
        foreach (char c in aText)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                default:
                    if (c < ' ' || (forceAscii && c > 127))
                    {
                        ushort val = c;
                        sb.Append("\\u").Append(val.ToString("X4"));
                    }
                    else
                        sb.Append(c);
                    break;
            }
        }
        string result = sb.ToString();
        //Log(result, "result");
        sb.Length = 0;
        return result;
    }
    static string GetMemberName(MemberInfo member)
    {
        if (member.IsDefined(typeof(DataMemberAttribute), true))
        {
            DataMemberAttribute dataMemberAttribute = (DataMemberAttribute)Attribute.GetCustomAttribute(member, typeof(DataMemberAttribute), true);
            if (!string.IsNullOrEmpty(dataMemberAttribute.Name))
                return dataMemberAttribute.Name;
        }

        return member.Name;
    }
}

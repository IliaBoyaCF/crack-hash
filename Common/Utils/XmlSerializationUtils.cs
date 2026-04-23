using System.Xml.Serialization;

namespace Common.Utils;

public static class XmlSerializationUtils
{

    public static string Serialize<T>(T obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, obj);
                return stringWriter.ToString();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error serializing object of type {typeof(T).Name}", ex);
        }
    }

    public static T? Deserialize<T>(string xmlString)
    {
        if (string.IsNullOrWhiteSpace(xmlString))
            throw new ArgumentException("XML string can't be empty", nameof(xmlString));

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var stringReader = new StringReader(xmlString))
            {
                return (T?)serializer.Deserialize(stringReader);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error deserializeing XML to type {typeof(T).Name}", ex);
        }
    }
}

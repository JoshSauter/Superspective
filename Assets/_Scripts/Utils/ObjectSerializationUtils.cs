using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ObjectSerializationUtils {
    //Extension class to provide serialize / deserialize methods to object.
    //src: http://stackoverflow.com/questions/1446547/how-to-convert-an-object-to-a-byte-array-in-c-sharp
    //NOTE: You need add [Serializable] attribute in your class to enable serialization
    public static class ObjectSerializationExtension {
        private static readonly byte[] NullMarker = Array.Empty<byte>();

        public static byte[] SerializeToByteArray(this object obj) {
            if (obj == null) return NullMarker;
            
            BinaryFormatter bf = new BinaryFormatter();
            using MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        public static T Deserialize<T>(this byte[] byteArray) where T : class {
            if (byteArray == null || byteArray.Length == 0) return null;

            using MemoryStream memStream = new MemoryStream(byteArray);
            BinaryFormatter binForm = new BinaryFormatter();
            return (T)binForm.Deserialize(memStream);
        }
    }
}
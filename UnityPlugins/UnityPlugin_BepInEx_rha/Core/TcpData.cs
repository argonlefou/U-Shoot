using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx_UShoot_Plugin;

namespace UnityPlugin_BepInEx.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public class TcpData
    {
        private BinaryWriter _Writer;
        private BinaryReader _Reader;
        private MemoryStream _MStream;

        private FieldInfo[] _DataFields;

        /// <summary>
        /// Usign Reflexion to automatically init data array to desired length
        /// </summary>
        public TcpData(int PlayerNumber)
        {
            try
            {
                //GetFields does not guarantee a fix order for the list of fields
                //To ensure a known order, adding a reordering condition to get the fields sorting by the MetadataToken property (= Declaration Order) or Alphabetical order
                //But it's better to get it by Name to be 100% sure of the order (member inheritance, etc...)
                //See https://github.com/dotnet/runtime/issues/19732
                _DataFields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(field => field.Name).ToArray();
                
                foreach (FieldInfo fi in _DataFields)
                {
                    //Only creating Player-based array. IF An array is already existing and initialized, do not iverwrite it
                    if (fi.FieldType.IsArray && fi.GetValue(this) == null)
                    {
                        object o = Array.CreateInstance(fi.FieldType.GetElementType(), PlayerNumber);
                        fi.SetValue(this, o);
                        UShoot_Plugin.MyLogger.LogMessage("TcpOutputData.Ctor() : Creating " + fi.FieldType.GetElementType().ToString() + " [" + PlayerNumber + "] " + fi.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                UShoot_Plugin.MyLogger.LogError("TcpOutputData.Ctor(): " + ex.Message.ToString());
            }
        }

        /// <summary>
        /// Serialize Class fields to Byte array
        /// Using Reflexion to convert each field to Byte[]
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            byte[] bResult = new byte[1];

            using (_MStream = new MemoryStream())
            {
                using (_Writer = new BinaryWriter(_MStream))
                {
                    foreach (FieldInfo fi in _DataFields)
                    {
                        if (fi.FieldType.IsArray)
                        {
                            Type ElementType = fi.FieldType.GetElementType();
                            Array a = (Array)fi.GetValue(this);
                            for (int i = 0; i < a.Length; i++)
                            {
                                WriteBytes(ElementType, a.GetValue(i));
                            }
                        }
                        else
                        {
                            WriteBytes(fi.FieldType, fi.GetValue(this));
                        }
                    }
                }
                bResult = _MStream.ToArray();
            }

            return bResult;
        }

        /// <summary>
        /// Filling the Fields from a byte array, using reflexion to get the fields in a known order
        /// </summary>
        public void Update(byte[] ReceivedData)
        {
            using (_MStream = new MemoryStream(ReceivedData))
            {
                using (_Reader = new BinaryReader(_MStream))
                {
                    foreach (FieldInfo fi in _DataFields)
                    {
                        if (fi.FieldType.IsArray)
                        {
                            Type ElementType = fi.FieldType.GetElementType();
                            Array a = (Array)fi.GetValue(this);
                            for (int i = 0; i < a.Length; i++)
                            {
                                a.SetValue(ReadBytes(ElementType), i);
                            }
                        }
                        else
                        {
                            fi.SetValue(this, ReadBytes(fi.FieldType));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// BinaryStream.Write() need a known Type to work
        /// </summary>
        private void WriteBytes(Type t, Object o)
        {
            if (t == typeof(bool))
                _Writer.Write((bool)o);
            else if (t == typeof(byte))
                _Writer.Write((byte)o);
            else if (t == typeof(char))
                _Writer.Write((char)o);
            else if (t == typeof(decimal))
                _Writer.Write((decimal)o);
            else if (t == typeof(double))
                _Writer.Write((double)o);
            else if (t == typeof(float))
                _Writer.Write((float)o);
            /*else if (t == typeof(nint))
                _Writer.Write((nint)o);
            else if (t == typeof(nuint))
                _Writer.Write((nuint)o);*/
            else if (t == typeof(long))
                _Writer.Write((long)o);
            else if (t == typeof(sbyte))
                _Writer.Write((sbyte)o);
            else if (t == typeof(short))
                _Writer.Write((short)o);
            else if (t == typeof(uint))
                _Writer.Write((uint)o);
            else if (t == typeof(ulong))
                _Writer.Write((ulong)o);
            else if (t == typeof(ushort))
                _Writer.Write((ushort)o);
            else if (t == typeof(int))
                _Writer.Write((int)o);
        }

        /// <summary>
        /// BinaryStream.Read() need a known Type to work
        /// </summary>
        private object ReadBytes(Type t)
        {
            if (t == typeof(bool))
                return _Reader.ReadBoolean();
            else if (t == typeof(byte))
                return _Reader.ReadByte();
            else if (t == typeof(char))
                return _Reader.ReadChar();
            else if (t == typeof(decimal))
                return _Reader.ReadDecimal();
            else if (t == typeof(double))
                return _Reader.ReadDouble();
            else if (t == typeof(float))
                return _Reader.ReadSingle();
            /*else if (t == typeof(nint))
                return _Reader.
            else if (t == typeof(nuint))
                return _Reader.*/
            else if (t == typeof(long))
                return _Reader.ReadInt64();
            else if (t == typeof(sbyte))
                return _Reader.ReadSByte();
            else if (t == typeof(short))
                return _Reader.ReadInt16();
            else if (t == typeof(uint))
                return _Reader.ReadUInt32();
            else if (t == typeof(ulong))
                return _Reader.ReadUInt64();
            else if (t == typeof(ushort))
                return _Reader.ReadUInt16();
            else if (t == typeof(int))
                return _Reader.ReadInt32();
            else
                return null;
        }

        public override string ToString()
        {
            string s = string.Empty;
            byte[] b = this.ToByteArray();
            for (int i = 0; i < b.Length; i++)
            {
                s += "0x" + b[i].ToString("X2") + " ";
            }
            return s;
        }
    }
}
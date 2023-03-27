using Loxifi;
using Penguin.Reflection;
using Penguin.Remote.Attributes;
using Penguin.Remote.Commands;
using Penguin.Remote.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Penguin.Remote
{
    public static class SerializationHelper
    {
        private static readonly ConcurrentDictionary<Type, int> _cache = new ConcurrentDictionary<Type, int>();

        private static IEnumerable<MemberInfo> GetMembers(Type type)
        {
            List<MemberInfo> members = new List<MemberInfo>();

            foreach (PropertyInfo property in type.GetProperties())
            {
                members.Add(property);
            }

            foreach (FieldInfo field in type.GetFields())
            {
                members.Add(field);
            }

            return members.OrderBy(GetMemberOrder);
        }

        private static int GetMemberOrder(MemberInfo m)
        {
            if (m.Name == nameof(TransmissionPackage.Payload))
            {
                return int.MaxValue;
            }

            if (m.Name == nameof(TransmissionPackage.PayloadSize))
            {
                return int.MinValue;
            }

            if (m.Name == nameof(TransmissionPackage.RemoteCommandKind))
            {
                return int.MinValue + 1;
            }

            if (m.GetCustomAttribute<SerializationData>() is SerializationData sd)
            {
                return sd.Order;
            }

            return 0;
        }

        public static IEnumerable<MemberInfo> GetSerializedHeaders(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            foreach (MemberInfo member in GetMembers(type))
            {
                if (member.Name == nameof(TransmissionPackage.Payload) || !IsSerialized(member))
                {
                    continue;
                }

                yield return member;
            }
        }

        public static int GetHeadersLength(Type type)
        {
            int headerSize = 0;

            foreach (MemberInfo member in GetSerializedHeaders(type))
            {
                headerSize += SizeOf(member);
            }

            return headerSize;
        }

        public static int GetPayloadSizeLength() => SizeOf(typeof(long));

        public static long GetPackageLength(byte[] metaHeader)
        {
            TransmissionMeta meta = Deserialize(metaHeader);

            Type packageType = TypeFactory.Default.GetTypeByFullName(meta.RemoteCommandKind);

            if (packageType == null)
            {
                Console.WriteLine($"Command type '{meta.RemoteCommandKind}' not found");
            }

            long size = GetHeaderSize(packageType);

            size += meta.PayloadSize;

            return size;
        }

        public static TransmissionMeta Deserialize(byte[] bytes) => Deserialize<TransmissionMeta>(bytes);

        public static int GetHeaderSize(Type t)
        {
            int size = 0;

            foreach (MemberInfo member in GetSerializedHeaders(t))
            {
                size += SizeOf(member);
            }

            return size;
        }

        public static object Deserialize(byte[] bytes, Type t)
        {
            long p = 0;

            TransmissionMeta command = Activator.CreateInstance(t) as TransmissionMeta;

            foreach (MemberInfo member in GetSerializedHeaders(t))
            {
                int size = SizeOf(member);

                byte[] mVal = new byte[size];

                Array.Copy(bytes, p, mVal, 0, size);

                object val = GetValue(mVal, member.FindType());

                member.SetValue(command, val);

                p += size;
            }

            if (command is TransmissionPackage tp)
            {
                Array.Copy(bytes, p, tp.Payload, 0, tp.Payload.Length);
            }

            return command;
        }

        public static T Deserialize<T>(byte[] bytes) where T : TransmissionMeta, new() => Deserialize(bytes, typeof(T)) as T;

        public static byte[] Serialize<T>(T remoteCommand) where T : TransmissionPackage
        {
            if (remoteCommand is null)
            {
                throw new ArgumentNullException(nameof(remoteCommand));
            }

            int headerSize = GetHeadersLength(remoteCommand.GetType());

            byte[] toReturn = new byte[headerSize + remoteCommand.PayloadSize];

            long p = 0;

            foreach (MemberInfo member in GetSerializedHeaders(remoteCommand.GetType()))
            {
                object mVal = member.GetValue(remoteCommand);

                byte[] vBytes = GetBytes(mVal);

                int size = SizeOf(member);

                vBytes.CopyTo(toReturn, p);

                p += size;
            }

            remoteCommand.Payload.CopyTo(toReturn, p);

            return toReturn;
        }

        private static ConcurrentDictionary<Type, MethodInfo> GetBytesCache = new ConcurrentDictionary<Type, MethodInfo>();
        private static ConcurrentDictionary<Type, Func<byte[], object>> GetValueCache = new ConcurrentDictionary<Type, Func<byte[], object>>();

        private static MethodInfo? GetBytesConvertMethod(Type t) => typeof(BitConverter).GetMethods().Where(m => m.Name == nameof(BitConverter.GetBytes) && m.GetParameters().Count(p => p.ParameterType == t) == 1).SingleOrDefault();

        private static Func<byte[], object>? GetValueConvertMethod(Type t)
        {
            foreach (MethodInfo mi in typeof(BitConverter).GetMethods())
            {
                if (mi.ReturnType != t)
                {
                    continue;
                }

                List<ParameterInfo> parameters = mi.GetParameters().ToList();

                if (!parameters.Any())
                {
                    continue;
                }

                if (parameters[0].ParameterType != typeof(byte[]))
                {
                    continue;
                }

                if (parameters.Count == 1)
                {
                    return (b) => mi.Invoke(null, new object[] { b });
                }
                else if (parameters.Count == 2)
                {
                    return (b) => mi.Invoke(null, new object[] { b, 0 });
                }
            }

            return null;
        }

        public static byte[] GetBytes(object o)
        {
            if (o is null)
            {
                throw new ArgumentNullException(nameof(o));
            }

            Type oType = o.GetType();

            if (o is string s)
            {
                return System.Text.Encoding.UTF8.GetBytes(s);
            }

            MethodInfo getBytes = GetBytesCache.GetOrAdd(oType, GetBytesConvertMethod);

            return getBytes != null ? (byte[])getBytes.Invoke(null, new object[] { o })! : throw new NotImplementedException();
        }

        public static object GetValue(byte[] b, Type memberType)
        {
            if (memberType == typeof(string))
            {
                return System.Text.Encoding.UTF8.GetString(b).Trim('\0');
            }

            Func<byte[], object> getValue = GetValueCache.GetOrAdd(memberType, GetValueConvertMethod);

            return getValue != null ? getValue.Invoke(b)! : throw new NotImplementedException();
        }

        public static int SizeOf(Type t)
        {
            return _cache.GetOrAdd(t, _ =>
            {
                DynamicMethod? dm = new("SizeOfType", typeof(int), new Type[0]);
                ILGenerator il = dm.GetILGenerator();
                il.Emit(OpCodes.Sizeof, t);
                il.Emit(OpCodes.Ret);
                return (int)dm.Invoke(null, null);
            });
        }

        public static bool IsSerialized(MemberInfo mi) => mi.GetCustomAttribute<DontSerialize>() is null;

        public static int SizeOf(MemberInfo mi)
        {
            if (!IsSerialized(mi))
            {
                return 0;
            }

            if (mi.GetCustomAttribute<SerializationData>() is SerializationData sd && sd.Size != 0)
            {
                return sd.Size;
            }
            else
            {
                if (mi is PropertyInfo pi)
                {
                    return SizeOf(pi.PropertyType);
                }
                else if (mi is FieldInfo fi)
                {
                    return SizeOf(fi.FieldType);
                }

                throw new NotImplementedException();
            }
        }
    }
}
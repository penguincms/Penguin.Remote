using System;
using System.Reflection;

namespace Penguin.Remote.Extensions
{
    public static class MemberInfoExtensions
    {
        public static object GetValue(this MemberInfo mi, object Target)
        {
            if (mi is PropertyInfo pi)
            {
                return pi.GetValue(Target);
            }

            if (mi is FieldInfo fi)
            {
                return fi.GetValue(Target);
            }

            throw new NotImplementedException();
        }

        public static void SetValue(this MemberInfo mi, object Target, object val)
        {
            if (mi is PropertyInfo pi)
            {
                if (pi.GetSetMethod() != null)
                {
                    pi.SetValue(Target, val);
                }

                return;
            }

            if (mi is FieldInfo fi)
            {
                fi.SetValue(Target, val);
                return;
            }

            throw new NotImplementedException();
        }

        public static Type FindType(this MemberInfo mi)
        {
            if (mi is PropertyInfo pi)
            {
                return pi.PropertyType;
            }

            if (mi is FieldInfo fi)
            {
                return fi.FieldType;
            }

            throw new NotImplementedException();
        }
    }
}
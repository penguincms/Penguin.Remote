using Penguin.Reflection;
using Penguin.Remote.Commands;
using Penguin.Remote.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Penguin.Remote
{
    public static class Functions
    {
        public static byte[] Execute(byte[] command)
        {
            TransmissionMeta dataPackage = SerializationHelper.Deserialize(command);

            Type realType = TypeFactory.GetTypeByFullName(dataPackage.RemoteCommandKind);

            if(realType == null)
            {
                Console.WriteLine($"Command type '{dataPackage.RemoteCommandKind}' not found");
            }

            object package = SerializationHelper.Deserialize(command, realType);

            Type responseType = null;

            Type checkType = realType;

            while (responseType is null && checkType != null)
            {
                responseType = checkType.GetGenericArguments().SingleOrDefault();
                checkType = checkType.BaseType;
            }

            MethodInfo method = typeof(Functions).GetMethods().SingleOrDefault(m => m.ReturnType == responseType && m.GetParameters().Single().ParameterType == realType);

            if(method is null)
            {
                throw new NotImplementedException();
            }

            ServerResponse response;

            try
            {
                response = method.Invoke(null, new object[] { package }) as ServerResponse;
            } catch(Exception ex)
            {
                response = Activator.CreateInstance(responseType) as ServerResponse;

                response.Success = false;

                if(ex.InnerException  != null)
                {
                    ex = ex.InnerException;
                }

                response.Text = ex.Message + System.Environment.NewLine + ex.StackTrace;
            }

            byte[] responseBytes = SerializationHelper.Serialize(response);

#if DEBUG
            StateObject state = new StateObject();
            state.Append(responseBytes);

            if(!state.IsComplete)
            {
                Debugger.Break();
            }


#endif

            return responseBytes;
        }

        public static EnumerateFilesResponse EnumerateFiles(EnumerateFiles request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return new EnumerateFilesResponse()
            {
                Files = Directory.EnumerateFiles(request.Path, request.Mask, request.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                 .Select(f => new FileInfo(f))
                                 .Select(f => new FileData() { FileName = f.FullName, Length = f.Length })
                                 .ToList()
            };
        }

        public static PushResponse Push(Push request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            FileInfo target = new(request.RemotePath);

            if (target.Exists)
            {
                if (!request.Overwrite)
                {
                    return new PushResponse()
                    {
                        Success = false,
                        Text = "The file already exists"
                    };
                }
                else
                {
                    File.Delete(request.RemotePath);
                } 
            }

            if(!target.Directory.Exists)
            {
                target.Directory.Create();
            }

            File.WriteAllBytes(request.RemotePath, request.Payload);

            return new PushResponse();
        }

        public static PullResponse Pull(Pull request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return new PullResponse()
            {
                Payload = File.ReadAllBytes(request.RemotePath)
            };
        }

        public static EchoResponse Echo(Echo request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            EchoResponse response = new();

            response.Text = request.Text;

            return response;
        }

    }
}

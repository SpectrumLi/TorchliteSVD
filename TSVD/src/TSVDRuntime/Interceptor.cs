// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace TSVDRuntime
{
    /// <summary>
    /// Interceptor class.
    /// </summary>
    public class Interceptor
    {
        /// <summary>
        /// Runtime configuration.
        /// </summary>
        private static readonly TSVDRuntimeConfiguration Configuration;
        private static readonly Dictionary<Guid, string> FieldNameDict = new Dictionary<Guid, string>();
        static Interceptor()
        {
            var workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(workingDirectory);
            string configPath = Path.Combine(workingDirectory, Constants.RuntimeConfigFile);
            Configuration = TSVDRuntimeConfiguration.Parse(configPath);
        }

        /// <summary>
        /// Method called right before an intercepted api.
        /// </summary>
        /// <param name="instance">Object instance of the called API.</param>
        /// <param name="method">Name of the method calling the api.</param>
        /// <param name="api">intercepeted API name.</param>
        /// <param name="ilOffset">ILOffset of the API call.</param>
        public static void OnStart(object instance, string method, string api, int ilOffset)
        {
            InterceptionPoint interceptionPoint = new InterceptionPoint()
            {
                Method = method,
                API = MethodSignatureWithoutReturnType(api),
                ILOffset = ilOffset,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
            };
            Configuration.TrapController.InterceptionPointStart(interceptionPoint, instance);
        }

        private static string MethodSignatureWithoutReturnType(string fullName)
        {
            string[] tokens = fullName.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            return tokens[tokens.Length - 1].Replace("::", ".");
        }

        /// <summary>
        /// Callback invoked before a field is written.
        /// </summary>
        /// <param name="parentObject">Object owning the field.</param>
        /// <param name="fieldName">Field name.</param>
        /// <param name="currentValue">Field value before the write.</param>
        /// <param name="newValue">Field value after the write.</param>
        /// <param name="caller">Method that writes the field.</param>
        /// <param name="ilOffset">ILOffset where the write happens.</param>
        public static void BeforeFieldWrite(object parentObject, string fieldName, object currentValue, object newValue, string caller, int ilOffset)
        {
            string uniqueFieldName = GetUniqueFieldId(parentObject, fieldName);
            Guid currValueId = ObjectId.GetRefId(currentValue);
            Guid newValueId = ObjectId.GetRefId(newValue);
            FieldNameDict[currValueId] = uniqueFieldName;

            InterceptionPoint interceptionPoint = new InterceptionPoint()
            {
                Method = caller,
                API = "Write " + MethodSignatureWithoutReturnType(fieldName),
                ILOffset = ilOffset,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                ObjID = uniqueFieldName,
                GroupName = fieldName,
                IsWrite = true,
            };
            Configuration.TrapController.InterceptionPointStart(interceptionPoint, parentObject);
            
            //Logger.Log($"BeforeFieldWrite\t{uniqueFieldName}\t{currValueId}\t{newValueId}\t{caller}\t{ilOffset}");
        }

        /// <summary>
        /// Callback invoked after a field is written.
        /// </summary>
        /// <param name="parentObject">Object owning the field.</param>
        /// <param name="fieldName">Field name.</param>
        /// <param name="fieldValue">Field value after the write.</param>
        /// <param name="caller">Method that writes the field.</param>
        /// <param name="ilOffset">ILOffset where the write happens.</param>
        public static void AfterFieldWrite(object parentObject, string fieldName, object fieldValue, string caller, int ilOffset)
        {
            //string uniqueFieldName = GetUniqueFieldId(parentObject, fieldName);
            //Guid currValueId = ObjectId.GetRefId(fieldValue);
            //FieldNameDict[currValueId] = uniqueFieldName;

            //Logger.Log($"AfterFieldWrite\t{uniqueFieldName}\t{currValueId}\t{caller}\t{ilOffset}");
        }

        /// <summary>
        /// Callback invoked before a field is read.
        /// </summary>
        /// <param name="parentObject">Object owning the field.</param>
        /// <param name="fieldName">Field name.</param>
        /// <param name="fieldValue">Field value.</param>
        /// <param name="caller">Method that writes the field.</param>
        /// <param name="ilOffset">ILOffset where the write happens.</param>
        public static void BeforeFieldRead(object parentObject, string fieldName, object fieldValue, string caller, int ilOffset)
        {
            string uniqueFieldName = GetUniqueFieldId(parentObject, fieldName);
            Guid objId = ObjectId.GetRefId(fieldValue);
            InterceptionPoint interceptionPoint = new InterceptionPoint()
            {
                Method = caller,
                API = "Read " + MethodSignatureWithoutReturnType(fieldName),
                ILOffset = ilOffset,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                ObjID = uniqueFieldName,
                GroupName = fieldName,
                IsWrite = false,
            };
            Configuration.TrapController.InterceptionPointStart(interceptionPoint, parentObject);

            //Logger.Log($"BeforeFieldRead\t{uniqueFieldName}\t{objId}\t{caller}\t{ilOffset}");
        }

        /// <summary>
        /// Callback invoked before a method is called.
        /// </summary>
        /// <param name="instance">Instance object on which the method is called. Null if the method is static.</param>
        /// <param name="caller">Parent method that calls the method.</param>
        /// <param name="ilOffset">ILOffset where the method is invoked.</param>
        /// <param name="callee">Name of the called method.</param>
        /// <returns>A context given to AfterMethodCall callback.</returns>
        public static MethodCallbackContext BeforeMethodCall(object instance, string caller, int ilOffset, string callee)
        {
            // string uniqueFieldName = GetUniqueFieldId(parentObject, fieldName);
            // Guid objId = ObjectId.GetRefId(instance);
            InterceptionPoint interceptionPoint = new InterceptionPoint()
            {
                Method = caller,
                API = "Read " + MethodSignatureWithoutReturnType(callee),
                ILOffset = ilOffset,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                ObjID = ObjectId.GetRefId(instance).ToString(),
                GroupName = "No-Group",
                IsWrite = false,
            };
            var objId = ObjectId.GetRefId(instance);
            string fieldId = "null";
            if (FieldNameDict.ContainsKey(objId))
            {
                fieldId = FieldNameDict[objId];
            }

            Configuration.TrapController.InterceptionPointStart(interceptionPoint, instance);
            return new MethodCallbackContext() { Instance = instance, FieldId = fieldId, Caller = caller, ILOffset = ilOffset, Callee = callee };
        }
        private static string GetUniqueFieldId(object parentObject, string fieldName)
        {
        Guid parentObjId = ObjectId.GetRefId(parentObject);
        return $"{parentObjId}_{fieldName}";
        }
        /// <summary>
        /// Callback invoked after a method call.
        /// </summary>
        /// <param name="methodCallContext">Call context returned by BeforeMethodCall callback.</param>
    
        public static void AfterMethodCall(object methodCallContext)
        {
        
        }
        private static string ExcludeObjectId(string fieldId)
        {
            int underScore = fieldId.IndexOf('_');
            return fieldId.Substring(underScore + 1);
        }
    }
}

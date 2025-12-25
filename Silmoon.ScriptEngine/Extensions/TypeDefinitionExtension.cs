using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Silmoon.ScriptEngine.Extensions
{
    public static class TypeDefinitionExtension
    {
        /// <summary>
        /// 获取所有接口，包括基类实现的接口
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<InterfaceImplementation> GetAllInterfaces(this TypeDefinition type)
        {
            // 使用 HashSet 记录接口名称，确保接口的唯一性
            var interfaces = new List<InterfaceImplementation>();
            var interfaceNames = new HashSet<TypeReference>();

            // 添加当前类型的接口
            if (type.HasInterfaces)
            {
                foreach (var iface in type.Interfaces)
                {
                    if (interfaceNames.Add(iface.InterfaceType)) interfaces.Add(iface);
                }
            }

            // 获取所有基类并检查它们的接口
            var baseTypes = type.GetAllBaseTypes();
            foreach (var baseType in baseTypes)
            {
                if (baseType.HasInterfaces)
                {
                    foreach (var baseInterface in baseType.Interfaces)
                    {
                        if (interfaceNames.Add(baseInterface.InterfaceType)) interfaces.Add(baseInterface);
                    }
                }
            }

            return interfaces;
        }

        /// <summary>
        /// 获取所有基类，迭代方式避免深度递归
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<TypeDefinition> GetAllBaseTypes(this TypeDefinition type)
        {
            var baseTypes = new List<TypeDefinition>();
            var current = type.BaseType;

            while (current != null)
            {
                try
                {
                    TypeDefinition? resolved = current.Resolve();
                    if (resolved is null) break;

                    baseTypes.Add(resolved);
                    current = resolved.BaseType;
                }
                catch { throw; }
            }

            return baseTypes;
        }
    }
}
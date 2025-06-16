using System.Reflection;
using System.Reflection.Emit;

namespace Mars.MetaModelGenerator;

public class EmitVariantExample
{
    public static void ModelBuilder()
    {
        // Создаем сборку и модуль в памяти
        AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        // Создаем тип (класс)
        TypeBuilder typeBuilder = moduleBuilder.DefineType("DynamicClass",
            TypeAttributes.Public | TypeAttributes.Class);

        // Добавляем поле
        FieldBuilder fieldBuilder = typeBuilder.DefineField("_value", typeof(int), FieldAttributes.Private);

        // Добавляем свойство
        PropertyBuilder propertyBuilder = typeBuilder.DefineProperty("Value",
            PropertyAttributes.HasDefault, typeof(int), null);

        // Создаем методы get и set для свойства
        MethodBuilder getMethodBuilder = typeBuilder.DefineMethod("get_Value",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(int), Type.EmptyTypes);

        ILGenerator getIl = getMethodBuilder.GetILGenerator();
        getIl.Emit(OpCodes.Ldarg_0);
        getIl.Emit(OpCodes.Ldfld, fieldBuilder);
        getIl.Emit(OpCodes.Ret);

        MethodBuilder setMethodBuilder = typeBuilder.DefineMethod("set_Value",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null, new Type[] { typeof(int) });

        ILGenerator setIl = setMethodBuilder.GetILGenerator();
        setIl.Emit(OpCodes.Ldarg_0);
        setIl.Emit(OpCodes.Ldarg_1);
        setIl.Emit(OpCodes.Stfld, fieldBuilder);
        setIl.Emit(OpCodes.Ret);

        // Привязываем методы к свойству
        propertyBuilder.SetGetMethod(getMethodBuilder);
        propertyBuilder.SetSetMethod(setMethodBuilder);

        // Создаем тип
        Type dynamicType = typeBuilder.CreateType();

        // Создаем экземпляр
        object instance = Activator.CreateInstance(dynamicType)!;

        // Устанавливаем значение свойства
        dynamicType.GetProperty("Value").SetValue(instance, 42);

        // Получаем значение свойства
        int value = (int)dynamicType.GetProperty("Value").GetValue(instance)!;
        Console.WriteLine(value); // Выведет: 42
    }
}

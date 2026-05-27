using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes.InlineFunctions;

public static class InlineFunctionsUtils
{
    [MethodInlineFunctionNodeDefine(Color = "#dcdc9b")]
    [Display(Name = "Random number", Description = "Generates a random number between min and max", GroupName = "math")]
    public static int RandomNumber(
        [Display(Description = "The inclusive lower bound of the random number returned."), Required] int min = 1,
        [Display(Description = "The exclusive upper bound of the random number returned. maxValue must be greater than or equal to minValue"), Required] int max = 10)
    {
        return Random.Shared.Next(min, max);
    }

    [MethodInlineFunctionNodeDefine(Color = "#dcdc9b")]
    [Display(Name = "Generate array", Description = "Generates an array of random numbers between min and max", GroupName = "math")]
    public static int[] GenerateArray(
        [Display(Description = "Number of elements in the array"), Required] int length = 10,
        [Display(Description = "The inclusive lower bound of the random number returned."), Required] int min = 1,
        [Display(Description = "The exclusive upper bound of the random number returned. maxValue must be greater than or equal to minValue"), Required] int max = 10)
    {
        var array = new int[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = Random.Shared.Next(min, max);
        }
        return array;
    }

    [MethodInlineFunctionNodeDefine(Color = "#dcdc9b")]
    [Display(Name = "Generate sequential array", Description = "Generates an array with sequential numbers from start to end", GroupName = "math")]
    public static int[] GenerateSequentialArray(
        [Display(Description = "Starting number"), Required] int start = 0,
        [Display(Description = "Ending number (inclusive)"), Required] int end = 10)
    {
        int length = end - start + 1;
        var array = new int[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = start + i;
        }
        return array;
    }

    [MethodInlineFunctionNodeDefine(Color = "#dcdc9b")]
    [Display(Name = "Generate constant array", Description = "Generates an array where all elements have the same value", GroupName = "math")]
    public static int[] GenerateConstantArray(
        [Display(Description = "Number of elements in the array"), Required] int length = 10,
        [Display(Description = "The value to fill the array with"), Required] int value = 0)
    {
        var array = new int[length];
        Array.Fill(array, value);
        return array;
    }

    //[MethodInlineFunctionNodeDefine(Color = "#dcdc9b")]
    //[Display(Name = "Max in array", Description = "Finds the maximum value in the array", GroupName = "math")]
    //public static int MaxInArray(int[] array)
    //{
    //    return array.Max();
    //}

    //[MethodInlineFunctionNodeDefine(Color = "#dcdc9b")]
    //[Display(Name = "Min in array", Description = "Finds the minimum value in the array", GroupName = "math")]
    //public static int MinInArray(int[] array)
    //{
    //    return array.Min();
    //}

    [MethodInlineFunctionNodeDefine(Color = "#7bc691")]
    [Display(Name = "Guid generator", Description = "Generates a new Guid", GroupName = "math")]
    public static Guid GuidGenerator()
    {
        return Guid.NewGuid();
    }

}

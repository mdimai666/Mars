using Xunit.Abstractions;

namespace Test.Mars.Host.Models;

#if false
public class IsDayOffTests
{
    private readonly ITestOutputHelper output;

    public TestIsDayOffTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    public class TestClass
    {
        public DateOnly BirthDate { get; set; }
        public DateTime SetBirthDate { get => BirthDate.ToDateTime(TimeOnly.MinValue); set => BirthDate = DateOnly.FromDateTime(value); }
    }

    [Fact]
    public void TestDateTimeSetter()
    {
        DateTime date1 = new DateTime(2022, 6, 2);
        DateOnly date2 = new DateOnly(2022, 6, 2);

        TestClass a1 = new TestClass() { BirthDate = date2 };
        TestClass a2 = new TestClass() { SetBirthDate = date1 };

        Assert.Equal(a1.BirthDate, a2.BirthDate);

    }

    [Fact]
    public async void TestIsDayOff()
    {
        var off = new IsDayOff();
        var data = await IsDayOff.GetYearInfo(DateTime.Now.Year);
        off.AssignData(data);

        DateTime freeday = new DateTime(DateTime.Now.Year, 5, 9);
        DateTime workday = new DateTime(DateTime.Now.Year, 5, 16);

        //DEBUG
        //int dayInYear = freeday.DayOfYear;//129
        //bool freeday9may = data[dayInYear - 1] == '1';

        //char d125 = data[125-1+124/*commas*/];
        //char d126 = data[126-1+125/*commas*/];
        //char d127 = data[127-1+126/*commas*/];
        //char d128 = data[128-1+127];
        //char d129 = data[129-1+128];
        //char d130 = data[130-1+129];


        Assert.True(off.GetIsDayOff(freeday) == true);
        Assert.True(off.GetIsDayOff(workday) == false);
    }

}

#endif

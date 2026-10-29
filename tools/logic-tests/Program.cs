using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

int passed = 0, failed = 0;
foreach (Type fixture in new[] { typeof(CombatRulesTests), typeof(BombStockTests), typeof(PickupClaimTests), typeof(GameModeRulesTests) })
{
    foreach (MethodInfo test in fixture.GetMethods().Where(method => method.IsDefined(typeof(TestAttribute), false)))
    {
        try
        {
            test.Invoke(Activator.CreateInstance(fixture), null);
            passed++;
            Console.WriteLine("PASS " + test.Name);
        }
        catch (Exception error)
        {
            failed++;
            Console.WriteLine("FAIL " + test.Name + ": " + (error.InnerException ?? error).Message);
        }
    }
}
Console.WriteLine($"Policy tests: {passed} passed, {failed} failed. Unity/network tests were not run.");
return failed == 0 ? 0 : 1;

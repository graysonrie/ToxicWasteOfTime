using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToxicWasteOfTime.PvZActions; 
internal class TestAction1(){

    public static void Execute(Xbox360ControllerAPI api) {
        var group1 = api.RecordActions();
        group1.PressMenu();
        group1.Wait(3000);
        group1.FlickLeftStick(0, -1);
        group1.WaitTrivial();
        group1.FlickLeftStick(0, -1);
        group1.WaitTrivial();
        group1.PressA();
        group1.Wait(1500);
        group1.HoldRightStick(-1, 0, 1000);
        group1.Execute();
    }
}

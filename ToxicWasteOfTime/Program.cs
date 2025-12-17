using System;
using System.Threading;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using ToxicWasteOfTime.PvZActions;
namespace ToxicWasteOfTime;

public class Program
{
    static void Main()
    {
        // Create a ViGEm client and an emulated Xbox 360 controller, then connect it.
        var client = new ViGEmClient();
        IXbox360Controller pad = client.CreateXbox360Controller();
        pad.Connect();

        Thread.Sleep(1000);

        // Create the API wrapper
        var api = new Xbox360ControllerAPI(pad);

        TestAction1.Execute(api);

        // Example 1: Sequential execution (with Wait between actions)
        //var actionGroup1 = api.RecordActions();
        //actionGroup1.HoldLeftStick(1.0, 0.5, 1000)  // Hold stick right and slightly up for 1 second
        //            .Wait(1000)                      // Wait 1 second
        //            .PressA()                        // Press A button (executes after HoldLeftStick completes)
        //            .Execute();

        //Thread.Sleep(500);

        //// Example 2: Parallel execution (no Wait between actions)
        //var actionGroup2 = api.RecordActions();
        //actionGroup2.HoldLeftStick(1.0, 0.5, 1000)  // Hold stick right and slightly up
        //            .PressA()                        // Press A (executes AT THE SAME TIME as HoldLeftStick)
        //            .Execute();

        //Thread.Sleep(500);

        //// Example 3: Mixed sequential and parallel
        //var actionGroup3 = api.RecordActions();
        //actionGroup3.PressB()                        // Press B
        //            .HoldRightTrigger(500)           // Hold right trigger (parallel with PressB)
        //            .Wait(200)                       // Wait 200ms (breaks parallel grouping)
        //            .FlickLeftStick(-1.0, 0.0)        // Quick flick left (sequential after wait)
        //            .PressX()                        // Press X (parallel with FlickLeftStick)
        //            .Execute();

        //Thread.Sleep(500);

        //// Example 4: Using async/await
        //var actionGroup4 = api.RecordActions();
        //actionGroup4.HoldLeftStick(0.0, 1.0, 2000)   // Hold stick up for 2 seconds
        //            .HoldRightStick(-1.0, 0.0, 2000) // Hold right stick left (parallel)
        //            .PressY()                        // Press Y (parallel)
        //            .Wait(500)
        //            .PressMenu()                     // Press Menu button
        //            .Execute();

        //Thread.Sleep(500);

        //// Example 5: D-Pad and triggers
        //var actionGroup5 = api.RecordActions();
        //actionGroup5.PressDPadRight()
        //            .PressLeftTrigger()
        //            .Wait(300)
        //            .PressDPadUp()
        //            .PressRightShoulder()
        //            .Execute();


        Thread.Sleep(1000);
        pad.Disconnect();
    }
}

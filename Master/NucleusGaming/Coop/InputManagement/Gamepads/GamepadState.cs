﻿using EasyHook;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Gaming.Coop.InputManagement.Gamepads
{
    public static class GamepadState
    {
        [DllImport("xinput1_4.dll", EntryPoint = "#100")]//Enable Menu button on controllers
        public static extern int XInputGetStateSecret
        (
                 int dwUserIndex,// [in] Index of the device
                 ref State pState// [out] Receives the current state
        );

        public static Controller[] Controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };

        public static int GetPressedButtons(int index)///Return current pressed button of any available XInput Controller
        {
            if (Controllers[index].IsConnected)
            {
                State state = (State)GetControllerState(index);                
                return (int)state.Gamepad.Buttons;
            }
           
            return 0;
        }

        public static int GetRightTriggerValue(int index)///Return current right trigger value of XInput Controller by index
        {
            if (Controllers[index].IsConnected)
            {
                State state = (State)GetControllerState(index);

                return (int)state.Gamepad.RightTrigger;
            }

            return 0;
        }

        public static int GetLeftTriggerValue(int index)///Return current left trigger value of XInput Controller by index
        {
            if (Controllers[index].IsConnected)
            {
                State state = (State)GetControllerState(index);

                return (int)state.Gamepad.LeftTrigger;
            }

            return 0;
        }

        public static (int, int) GetRightStickValue(int index)///Return current right stick value of XInput Controller by index
        {
            if (Controllers[index].IsConnected)
            {
                State state = (State)GetControllerState(index);

                return ((int)state.Gamepad.RightThumbX, (int)state.Gamepad.RightThumbY);
            }

            return (0, 0);
        }

        public static (int, int) GetLeftStickValue(int index)///Return current left stick value of XInput Controller by index
        {
            if (Controllers[index].IsConnected)
            {
                State state = (State)GetControllerState(index);

                return ((int)state.Gamepad.LeftThumbX, (int)state.Gamepad.LeftThumbY);
            }

            return (0, 0);
        }

        public static Object GetControllerState(int index)///Return current left stick value of XInput Controller by index
        {
            Controller controller = Controllers[index];

            if (controller.IsConnected)
            {
                var state = controller.GetState();

                XInputGetStateSecret(index, ref state);

                int.TryParse("None", out int noButtonPressed);

                return state;
            }

            return new State();
        }
    }
}

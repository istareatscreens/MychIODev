using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.IO.Ports;
using MychIO.Device;

public class TestBoard : MonoBehaviour {
    private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

    private GameObject[] _touchIndicators = null;
    private GameObject[] _buttonIndicators = null;

    void Start(){
        // Setup Adx Connection:
        var touchPanelCallbacks = new Dictionary<TouchPanelZone, Action<TouchPanelZone, Enum>>();
        var buttonRingCallbacks = new Dictionary<ButtonRingZone, Action<ButtonRingZone, Enum>>();

        // Setup callbacks
        foreach (GameObject touchIndicator in _touchIndicators)
        {
            touchIndicator.SetActive(false);
            if (!Enum.TryParse(touchIndicator.name, true, out TouchPanelZone touchZone))
            {
                throw new Exception($"failed to connect to  {touchIndicator.name}");
            }
            touchPanelCallbacks[touchZone] = (TouchPanelZone input, Enum state) =>
            {
                touchIndicator.SetActive((InputState)state == InputState.On);
            };
        }

        foreach (GameObject buttonIndicator in _buttonIndicators)
        {
            buttonIndicator.SetActive(false);
            if (!Enum.TryParse(buttonIndicator.name, true, out ButtonRingZone buttonZone))
            {
                throw new Exception($"failed to connect to  {buttonIndicator.name}");
            }
            buttonRingCallbacks[buttonZone] = (ButtonRingZone input, Enum state) =>
            {
                buttonIndicator.SetActive((InputState)state == InputState.On);
            };
        }
        // TODO: Instantiate IO and connect devices
    }
    
}
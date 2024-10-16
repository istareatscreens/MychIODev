using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MychIO.Device;
using MychIO;
using MychIO.Event;

public class TestBoard : MonoBehaviour
{

    // handle concurrency
    private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

    private GameObject[] _touchIndicators = null;
    private GameObject[] _buttonIndicators = null;
    private IOManager _ioManager;

    // Display

    [SerializeReference]
    private TextMeshProUGUI _eventText;

    void Start()
    {

        // load debug menus
        _eventText.text = "Waiting for events...";

        // Load game objects
        _touchIndicators = GameObject.FindGameObjectsWithTag("TouchIndicator");
        _buttonIndicators = GameObject.FindGameObjectsWithTag("ButtonIndicator");
        _ioManager = new IOManager();

        foreach (GameObject touchIndicator in _touchIndicators)
        {
            touchIndicator.SetActive(false);
        }

        foreach (GameObject buttonIndicator in _buttonIndicators)
        {
            buttonIndicator.SetActive(false);
        }
    }

    public void connectDevices()
    {
        // Setup Adx Connection:
        var touchPanelCallbacks = new Dictionary<TouchPanelZone, Action<TouchPanelZone, InputState>>();
        var buttonRingCallbacks = new Dictionary<ButtonRingZone, Action<ButtonRingZone, InputState>>();

        // Setup callbacks
        foreach (GameObject touchIndicator in _touchIndicators)
        {
            if (!Enum.TryParse(touchIndicator.name, true, out TouchPanelZone touchZone))
            {
                throw new Exception($"failed to connect to  {touchIndicator.name}");
            }
            touchPanelCallbacks[touchZone] = (TouchPanelZone input, InputState state) =>
            {
                var currentIndicator = touchIndicator.gameObject;
                _executionQueue.Enqueue(() =>
                {
                    currentIndicator.SetActive(state == InputState.On);
                });
            };
        }

        foreach (GameObject buttonIndicator in _buttonIndicators)
        {
            if (!Enum.TryParse(buttonIndicator.gameObject.name, true, out ButtonRingZone buttonZone))
            {
                throw new Exception($"failed to connect to  {buttonIndicator.name}");
            }
            buttonRingCallbacks[buttonZone] = (ButtonRingZone input, InputState state) =>
            {
                var currentIndicator = buttonIndicator.gameObject;
                _executionQueue.Enqueue(() =>
                {
                    currentIndicator.gameObject.SetActive(state == InputState.On);
                });
            };
        }

        // Setup Events
        var eventCallbacks = new Dictionary<IOEventType, ControllerEventDelegate>{
                    { IOEventType.Attach,
                        (eventType, deviceType, message) =>
                        {
                            appendEventText($"eventType: {eventType} type: {deviceType} message: {message.Trim()}");
                        }
                    },
                    { IOEventType.ConnectionError,
                        (eventType, deviceType, message) =>
                        {
                            appendEventText($"eventType: {eventType} type: {deviceType} message: {message.Trim()}");
                        }
                    },
                    { IOEventType.Debug,
                        (eventType, deviceType, message) =>
                        {
                            appendEventText($"eventType: {eventType} type: {deviceType} message: {message.Trim()}");
                        }
                    },
                    { IOEventType.Detach,
                        (eventType, deviceType, message) =>
                        {
                            appendEventText($"eventType: {eventType} type: {deviceType} message: {message.Trim()}");
                        }
                    },
                    { IOEventType.SerialDeviceReadError,
                        (eventType, deviceType, message) =>
                        {
                            appendEventText($"eventType: {eventType} type: {deviceType} message: {message.Trim()}");
                        }
                    }
                };

        _ioManager.Destroy(); // reset everything

        _ioManager.SubscribeToEvents(eventCallbacks);
        Task.Run(async () =>
        {
            try
            {
                await _ioManager
                    .AddTouchPanel(
                        AdxTouchPanel.GetDeviceName(),
                        inputSubscriptions: touchPanelCallbacks
                    );
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            /*
                        await ioManager
                        .AddButtonRing(
                            AdxTouchPanel.GetDeviceName(),
                            inputSubscriptions: buttonRingCallbacks
                        );
            */
            Debug.Log("Devices connected");
        });

        Debug.Log("HERE");
    }


    private void appendEventText(string message)
    {
        _executionQueue.Enqueue(() =>
            {
                _eventText.text = $"\n{DateTime.Now.ToString("HH:mm:ss:fff")} - {message}";
            }
        );
    }

    // Update is called once per frame
    void Update()
    {
        while (_executionQueue.TryDequeue(out var action))
        {
            action();
        }
    }

}
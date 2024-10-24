using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MychIO.Device;
using MychIO;
using MychIO.Event;
using System.Linq;

public class TestBoard : MonoBehaviour
{

    // handle concurrency
    private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

    private GameObject[] _touchIndicators = null;
    private Dictionary<TouchPanelZone, GameObject> _touchIndicatorMap;
    private GameObject[] _buttonIndicators = null;
    private Dictionary<ButtonRingZone, GameObject> _buttonIndicatorMap;
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

        _touchIndicatorMap = _touchIndicators.ToDictionary(go => Enum.Parse<TouchPanelZone>(go.name), go => go);

        _buttonIndicators = GameObject.FindGameObjectsWithTag("ButtonIndicator");

        _buttonIndicatorMap = _buttonIndicators
            .ToDictionary(go => Enum.Parse<ButtonRingZone>(go.name), go => go);

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

        // Setup callbacks for TouchPanelZone
        foreach (TouchPanelZone touchZone in System.Enum.GetValues(typeof(TouchPanelZone)))
        {
            if (!_touchIndicatorMap.TryGetValue(touchZone, out var touchIndicator))
            {
                throw new Exception($"Failed to find GameObject for {touchZone}");
            }

            touchPanelCallbacks[touchZone] = (TouchPanelZone input, InputState state) =>
            {
                _executionQueue.Enqueue(() =>
                {
                    touchIndicator.SetActive(state == InputState.On);
                });
            };
        }

        // Setup callbacks for ButtonRingZone
        foreach (ButtonRingZone buttonZone in System.Enum.GetValues(typeof(ButtonRingZone)))
        {
            if (!_buttonIndicatorMap.TryGetValue(buttonZone, out var buttonIndicator))
            {
                throw new Exception($"Failed to find GameObject for {buttonZone}");
            }

            buttonRingCallbacks[buttonZone] = (ButtonRingZone input, InputState state) =>
            {
                _executionQueue.Enqueue(() =>
                {
                    buttonIndicator.SetActive(state == InputState.On);
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
                await _ioManager.AddButtonRing(
                    AdxIO4ButtonRing.GetDeviceName(),
                    inputSubscriptions: buttonRingCallbacks
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
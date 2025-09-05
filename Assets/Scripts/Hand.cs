using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class Hand : MonoBehaviour
{
    public InputDeviceCharacteristics inputDeviceCharacteristics;

    private InputDevice _targetDevice;
    [SerializeField] private Animator _handAnimator;

    private void Start()
    {
        InitializeHand();
    }

    private void InitializeHand()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(inputDeviceCharacteristics, devices);

        if (devices.Count > 0)
        {
            _targetDevice = devices[0];
        }
    }

    private void Update()
    {
        if (!_targetDevice.isValid)
        {
            InitializeHand();
        }
        else
        {
            UpdateHand();
        }
    }

    private void UpdateHand()
    {
        SetAnimatorParameterValue(CommonUsages.grip, "Grip");
    }

    private void SetAnimatorParameterValue(InputFeatureUsage<float> feature, string parameterName)
    {
        if (_targetDevice.TryGetFeatureValue(feature, out float value))
        {
            _handAnimator.SetFloat(parameterName, value);
        }
        else
        {
            _handAnimator.SetFloat(parameterName, 0);
        }
    }
}
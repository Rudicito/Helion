using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using EvDevSharp;
using Helion.Geometry.Vectors;
using Helion.Util.Loggers;
using MtSlot = int;

namespace Helion.Window.Input;

public class TouchAdapter : ITouchAdapter
{
    private EvDevDevice touchScreen;
    
    /// <summary>
    /// Dictionary to get raw touch input value.<br/>
    /// The key is the MtSlot.<br/>
    /// The value is in the format 19200, which means 1920.0px.
    /// </summary>
    private readonly Dictionary<MtSlot, Vec2I> touchesByMtSlot = new();

    private readonly List<Vec2F> touchesList = new();
    
    /// <summary>
    /// The list of touches (fingers) on the screen
    /// </summary>
    public IReadOnlyList<Vec2F> GetTouch => touchesList;

    private MtSlot? currentMtSlot;

    [SupportedOSPlatform("linux")]
    public TouchAdapter(InputManager inputManager)
    {
        inputManager.TouchAdapter = this;

        touchScreen = EvDevDevice.GetDevices().First(d => d.GuessedDeviceType == EvDevGuessedDeviceType.TouchScreen && d.Name == "Touch passthrough");

        touchScreen.OnAbsoluteEvent += (_, e) =>
        {
            // Super good doc: https://www.kernel.org/doc/Documentation/input/multi-touch-protocol.txt
            switch (e.Axis)
            {
                case EvDevAbsoluteAxisCode.ABS_MT_SLOT:
                    currentMtSlot = e.Value;
                    break;

                case EvDevAbsoluteAxisCode.ABS_MT_POSITION_X:
                    if (currentMtSlot.HasValue)
                        AddOrUpdateX(currentMtSlot.Value, e.Value);
                    break;

                case EvDevAbsoluteAxisCode.ABS_MT_POSITION_Y:
                    if (currentMtSlot.HasValue)
                        AddOrUpdateY(currentMtSlot.Value, e.Value);
                    break;

                case EvDevAbsoluteAxisCode.ABS_MT_TRACKING_ID:
                    if (currentMtSlot.HasValue && e.Value == -1)
                        Remove(currentMtSlot.Value);
                    break;
            }
        };
        
        touchScreen.StartMonitoring();
    }
    
    public void Poll()
    {
        ReconstructList();
    }

    private void ReconstructList()
    {
        touchesList.Clear();
        touchesList.AddRange(touchesByMtSlot.Values.Select(vec2I => new Vec2F(vec2I.X / 10f, vec2I.Y / 10f)));
    }

    private void AddOrUpdateX(MtSlot mtSlot, int x)
    {
        if (touchesByMtSlot.TryGetValue(mtSlot, out Vec2I value))
            touchesByMtSlot[mtSlot] = new Vec2I(x, value.Y);
        else
            touchesByMtSlot[mtSlot] = new Vec2I(x, 0);
    }
    
    private void AddOrUpdateY(MtSlot mtSlot, int y)
    {
        if (touchesByMtSlot.TryGetValue(mtSlot, out Vec2I value))
            touchesByMtSlot[mtSlot] = new Vec2I(value.X, y);
        else
            touchesByMtSlot[mtSlot] = new Vec2I(0, y);
    }

    private void Remove(MtSlot mtSlot)
    {
        if (!touchesByMtSlot.Remove(mtSlot))
            HelionLog.Debug("TouchAdapter tried remove unexisted slot : " + mtSlot);
    }

    private string TouchesByMtSlotToString()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kvp in touchesByMtSlot)
        {
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"MtSlot: {kvp.Key}, Value : ({kvp.Value.X}, {kvp.Value.Y})");
        }
        
        sb.AppendLine();
        
        return sb.ToString();
    }
}
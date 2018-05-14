

using System;
using Sitecore.Analytics;
using Sitecore.CES.DeviceDetection;
using Sitecore.CES.DeviceDetection.Exceptions;
using Sitecore.CES.DeviceDetection.Rules;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Devices;
using Sitecore.Xdb.Configuration;

namespace Sitecore.Support.CES.DeviceDetection.Rules
{
  internal class RuleDeviceInformationManager : IRuleDeviceInformationManager
  {
    public DeviceInformation GetDeviceInformation(RuleContext ruleContext)
    {
      if (!DeviceDetectionManager.IsReady)
      {
        return null;
      }
      return this.ResolveObject<DeviceInformation>(ruleContext, "DeviceInfo", new Func<string, DeviceInformation>(DeviceDetectionManager.GetDeviceInformation), "Tracking is disabled. Device rules can't be evaluated", "Can't get the device info", Array.Empty<object>());
    }

    public string GetExtendedProperty(RuleContext ruleContext, string propertyName)
    {
      Assert.ArgumentNotNull(propertyName, "propertyName");
      if (!DeviceDetectionManager.IsReady)
      {
        return null;
      }
      object[] formatArgs = new object[] { propertyName };
      return this.ResolveObject<string>(ruleContext, "DeviceInfo" + propertyName, x => DeviceDetectionManager.GetExtendedProperty(x, propertyName), "Tracking is disabled. Device rules can't be evaluated", "Can't get the extended property '{0}'", formatArgs);
    }

    private string GetUserAgentFromDeviceRuleContext(DeviceRuleContext deviceRuleContext)
    {
      Assert.ArgumentNotNull(deviceRuleContext, "deviceRuleContext");
      Assert.IsNotNull(deviceRuleContext.HttpContext, "deviceRuleContext.HttpContext is null");
      Assert.IsNotNull(deviceRuleContext.HttpContext.Request, "deviceRuleContext.HttpContext.Request is null");
      return deviceRuleContext.HttpContext.Request.UserAgent;
    }

    private string GetUserAgentFromTracker()
    {
      Assert.IsNotNull(Tracker.Current, "Tracker.Current is not initialized");
      Assert.IsNotNull(Tracker.Current.Session, "Tracker.Current.Session is not initialized");
      Assert.IsNotNull(Tracker.Current.Session.Interaction, "Tracker.Current.Session.Interaction is not initialized");
      return Tracker.Current.Session.Interaction.UserAgent;
    }

    private T ResolveObject<T>(RuleContext ruleContext, string key, Func<string, T> objectFactory, string trackerDisabledMessageFormat, string deviceDetectionExceptionMessageFormat, params object[] formatArgs)
    {
      Assert.ArgumentNotNull(key, "key");
      Assert.ArgumentNotNull(objectFactory, "objectFactory");
      Assert.ArgumentNotNull(trackerDisabledMessageFormat, "trackerDisabledMessageFormat");
      Assert.ArgumentNotNull(deviceDetectionExceptionMessageFormat, "deviceDetectionExceptionMessageFormat");
      Assert.ArgumentNotNull(formatArgs, "formatArgs");
      try
      {
        DeviceRuleContext deviceRuleContext = ruleContext as DeviceRuleContext;
        if (deviceRuleContext != null)
        {
          object obj2;
          if (!deviceRuleContext.CustomData.TryGetValue(key, out obj2))
          {
            string userAgentFromDeviceRuleContext = this.GetUserAgentFromDeviceRuleContext(deviceRuleContext);
            if (userAgentFromDeviceRuleContext == null)
            {
              return default(T);
            }
            obj2 = objectFactory(userAgentFromDeviceRuleContext);
            deviceRuleContext.CustomData[key] = obj2;
          }
          return (T)obj2;
        }
        if (XdbSettings.Tracking.Enabled)
        {
          string userAgentFromTracker = this.GetUserAgentFromTracker();
          if (userAgentFromTracker == null)
          {
            return default(T);
          }
          return objectFactory(userAgentFromTracker);
        }
        Log.Warn(string.Format(trackerDisabledMessageFormat, formatArgs), this);
      }
      catch (DeviceDetectionException exception)
      {
        Log.Error(string.Format(deviceDetectionExceptionMessageFormat, formatArgs), exception, ruleContext);
      }
      return default(T);
    }
  }
}

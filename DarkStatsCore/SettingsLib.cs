using System;
using System.Linq;
using System.IO;
using DarkStatsCore.Data;
using Microsoft.EntityFrameworkCore;

public class SettingsLib
{
    public bool InvalidSettings => string.IsNullOrEmpty(Url);
    public string Url
    {
        get
        {
            try
            {
                return GetSetting(_urlKey).StringValue;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
    public TimeSpan SaveTime
    {
        get
        {
            try
            {
                return TimeSpan.FromSeconds(GetSetting(_saveTimeKey).IntValue);
            }
            catch
            {
                return TimeSpan.FromSeconds(300);
            }
        }
    }
    public TimeSpan DashboardRefreshTime
    {
        get
        {
            try
            {
                return TimeSpan.FromMilliseconds(GetSetting(_dashboardRefreshTimeKey).DoubleValue);
            }
            catch
            {
                return TimeSpan.FromSeconds(1);
            }
        }
    }
    public static string VersionInformation = GetFileString("BUILD_VERSION") ?? "Self compiled";

    private static string GetFileString(string fileName)
    {
        try
        {
            return File.ReadAllText(fileName).Replace(Environment.NewLine, string.Empty).Trim();
        }
        catch
        {
            return null;
        }
    }

    private string _urlKey = "Url";
    private string _saveTimeKey = "SaveTime";
    private string _dashboardRefreshTimeKey = "DashboardRefreshTime";
    private DarkStatsDbContext _context;
    private Settings GetSetting(string settingName) => _context.Settings.FirstOrDefault(s => s.Name == settingName);

    public SettingsLib(DarkStatsDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        if (!_context.Settings.Any())
        {
            SetSaveTime(15);
            SetDashboardRefreshTime(1000);
        }
    }    

    private void SaveSetting(Settings setting)
    {
        var currentSetting = GetSetting(setting.Name);
        if (currentSetting == null)
        {
            _context.Add(setting);
        }
        else
        {
            currentSetting.DoubleValue = setting.DoubleValue;
            currentSetting.IntValue = setting.IntValue;
            currentSetting.StringValue = setting.StringValue;
            _context.Update(currentSetting);
        }
        _context.SaveChanges();
    }

    public void SetUrl(string url)
    {
        try
        {
            var filteredUrl = new Uri(url).ToString();
            SaveSetting(new Settings
            {
                Name = _urlKey,
                StringValue = filteredUrl
            });
        }
        catch (Exception e)
        {
            throw new Exception("Error saving URL: " + e.Message);
        }
    }

    public void SetSaveTime(int saveTime)
    {
        try
        {
            SaveSetting(new Settings
            {
                Name = _saveTimeKey,
                IntValue = saveTime
            });
        }
        catch (Exception e)
        {
            throw new Exception("Error saving Save Time: " + e.Message);
        }
    }

    public void SetDashboardRefreshTime(double dashboardRefreshTime)
    {
        try
        {
            SaveSetting(new Settings
            {
                Name = _dashboardRefreshTimeKey,
                DoubleValue = dashboardRefreshTime
            });
        }
        catch (Exception e)
        {
            throw new Exception("Error saving URL: " + e.Message);
        }
    }
}
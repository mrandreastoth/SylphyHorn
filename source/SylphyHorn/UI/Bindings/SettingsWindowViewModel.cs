﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using JetBrains.Annotations;
using Livet;
using Livet.Messaging.IO;
using MetroRadiance.Platform;
using MetroTrilithon.Lifetime;
using MetroTrilithon.Mvvm;
using SylphyHorn.Properties;
using SylphyHorn.Serialization;
using SylphyHorn.Services;

namespace SylphyHorn.UI.Bindings
{
	public class SettingsWindowViewModel : WindowViewModel
	{
		private static bool _restartRequired;
		private static readonly string _defaultCulture = Settings.General.Culture;

		private readonly HookService _hookService;
		private readonly Startup _startup;

		public IReadOnlyCollection<DisplayViewModel<string>> Cultures { get; }

		public IReadOnlyCollection<DisplayViewModel<WindowPlacement>> Placements { get; }

		public bool IsDisplayEnabled { get; }

		public IReadOnlyCollection<DisplayViewModel<uint>> Displays { get; }

		public IReadOnlyCollection<BindableTextViewModel> Libraries { get; }

		public bool RestartRequired => _restartRequired;

		#region HasStartupLink notification property

		private bool _HasStartupLink;

		public bool HasStartupLink
		{
			get => this._HasStartupLink;
			set
			{
				if (this._HasStartupLink != value)
				{
					if (value)
					{
						this._startup.Create();
					}
					else
					{
						this._startup.Remove();
					}

					this._HasStartupLink = this._startup.IsExists;
					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

		#region Culture notification property

		public string Culture
		{
			get => Settings.General.Culture;
			set
			{
				if (Settings.General.Culture != value)
				{
					Settings.General.Culture.Value = value;
					_restartRequired = value != _defaultCulture;

					this.RaisePropertyChanged();
					this.RaisePropertyChanged(nameof(this.RestartRequired));
				}
			}
		}

		#endregion

		#region Placement notification property

		public WindowPlacement Placement
		{
			get => (WindowPlacement)Settings.General.Placement.Value;
			set
			{
				if ((WindowPlacement)Settings.General.Placement.Value != value)
				{
					Settings.General.Placement.Value = (uint)value;

					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

		#region Display notification property

		public uint Display
		{
			get => Settings.General.Display;
			set
			{
				if (Settings.General.Display != value)
				{
					Settings.General.Display.Value = value;

					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

		#region Backgrounds notification property

		private WallpaperFile[] _Backgrounds;

		public WallpaperFile[] Backgrounds
		{
			get => this._Backgrounds;
			set
			{
				if (this._Backgrounds != value)
				{
					this._Backgrounds = value;
					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

		public bool HasWallpaper => !string.IsNullOrEmpty(this.PreviewBackgroundPath);

		#region PreviewBackgroundBrush notification property

		private SolidColorBrush _PreviewBackgroundBrush;

		public SolidColorBrush PreviewBackgroundBrush
		{
			get => this._PreviewBackgroundBrush;
			set
			{
				if (this._PreviewBackgroundBrush != value)
				{
					this._PreviewBackgroundBrush = value;

					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

		#region PreviewBackgroundPath notification property

		private string _PreviewBackgroundPath;

		public string PreviewBackgroundPath
		{
			get => this._PreviewBackgroundPath;
			set
			{
				if (this._PreviewBackgroundPath != value)
				{
					this._PreviewBackgroundPath = value;

					this.RaisePropertyChanged();
					this.RaisePropertyChanged(nameof(this.HasWallpaper));
				}
			}
		}

		#endregion

		public Brush NotificationBackground => new SolidColorBrush(WindowsTheme.ColorPrevalence.Current
			? ImmersiveColor.GetColorByTypeName(ImmersiveColorNames.SystemAccentDark1)
			: ImmersiveColor.GetColorByTypeName(ImmersiveColorNames.DarkChromeMedium))
		{ Opacity = WindowsTheme.Transparency.Current ? 0.6 : 1.0 };

		public Brush NotificationForeground => new SolidColorBrush(ImmersiveColor.GetColorByTypeName(ImmersiveColorNames.SystemTextDarkTheme));

		public Brush TaskbarBackground => new SolidColorBrush(WindowsTheme.ColorPrevalence.Current
			? ImmersiveColor.GetColorByTypeName(ImmersiveColorNames.SystemAccentDark1)
			: ImmersiveColor.GetColorByTypeName(ImmersiveColorNames.DarkChromeMedium))
		{ Opacity = WindowsTheme.Transparency.Current ? 0.8 : 1.0 };

		public ReadOnlyDispatcherCollection<LogViewModel> Logs { get; }

		public SettingsWindowViewModel(HookService hookService)
		{
			this._hookService = hookService;
			this._startup = new Startup();

			this.Cultures = new[] { new DisplayViewModel<string> { Display = "(auto)", } }
				.Concat(ResourceService.Current.SupportedCultures
					.Select(x => new DisplayViewModel<string> { Display = x.NativeName, Value = x.Name, })
					.OrderBy(x => x.Display))
				.ToList();

			this.Placements = new[]
			{
				new DisplayViewModel<WindowPlacement> { Display = Resources.Settings_NotificationWindowPlacement_TopLeft, Value = WindowPlacement.TopLeft, },
				new DisplayViewModel<WindowPlacement> { Display = Resources.Settings_NotificationWindowPlacement_TopCenter, Value = WindowPlacement.TopCenter, },
				new DisplayViewModel<WindowPlacement> { Display = Resources.Settings_NotificationWindowPlacement_TopRight, Value = WindowPlacement.TopRight, },
				new DisplayViewModel<WindowPlacement> { Display = Resources.Settings_NotificationWindowPlacement_Center, Value = WindowPlacement.Center, },
				new DisplayViewModel<WindowPlacement> { Display = Resources.Settings_NotificationWindowPlacement_BottomLeft, Value = WindowPlacement.BottomLeft, },
				new DisplayViewModel<WindowPlacement> { Display = Resources.Settings_NotificationWindowPlacement_BottomCenter, Value = WindowPlacement.BottomCenter, },
				new DisplayViewModel<WindowPlacement> { Display = Resources.Settings_NotificationWindowPlacement_BottomRight, Value = WindowPlacement.BottomRight, },
			}.ToList();

			this.Displays = new[] { new DisplayViewModel<uint> { Display = Resources.Settings_MultipleDisplays_CurrentDisplay, Value = 0, } }
				.Concat(MonitorService.GetMonitors()
					.Select((m, i) => new DisplayViewModel<uint>
					{
						Display = string.Format(Resources.Settings_MultipleDisplays_EachDisplay, i + 1, m.Name),
						Value = (uint)(i + 1),
					}))
				.Concat(new[]
				{
					new DisplayViewModel<uint>
					{
						Display = Resources.Settings_MultipleDisplays_AllDisplays,
						Value = uint.MaxValue,
					}
				})
				.ToList();
			if (this.Displays.Count > 3) this.IsDisplayEnabled = true;

			this.Libraries = ProductInfo.Libraries.Aggregate(
				new List<BindableTextViewModel>(),
				(list, lib) =>
				{
					list.Add(new BindableTextViewModel { Text = list.Count == 0 ? "Build with " : ", ", });
					list.Add(new HyperlinkViewModel { Text = lib.Name.Replace(' ', Convert.ToChar(160)), Uri = lib.Url, });
					return list;
				},
				list =>
				{
					list.Add(new BindableTextViewModel() { Text = ".", });
					return list;
				});

			this._HasStartupLink = this._startup.IsExists;

			Settings.General.DesktopBackgroundFolderPath
				.Subscribe(path => this.Backgrounds = WallpaperService.Instance.GetWallpaperFiles(path))
				.AddTo(this);

			var colAndWall = WallpaperService.GetCurrentColorAndWallpaper();
			this.PreviewBackgroundBrush = new SolidColorBrush(colAndWall.Item1);
			this.PreviewBackgroundPath = colAndWall.Item2;

			this.Logs = ViewModelHelper.CreateReadOnlyDispatcherCollection(
				LoggingService.Instance.Logs,
				log => new LogViewModel(log),
				DispatcherHelper.UIDispatcher);

			WindowsTheme.ColorPrevalence
				.RegisterListener(_ => this.RaisePropertyChanged(nameof(this.NotificationBackground)))
				.AddTo(this);
			WindowsTheme.ColorPrevalence
				.RegisterListener(_ => this.RaisePropertyChanged(nameof(this.TaskbarBackground)))
				.AddTo(this);
			WindowsTheme.Transparency
				.RegisterListener(_ => this.RaisePropertyChanged(nameof(this.NotificationBackground)))
				.AddTo(this);
			WindowsTheme.Transparency
				.RegisterListener(_ => this.RaisePropertyChanged(nameof(this.TaskbarBackground)))
				.AddTo(this);

			Disposable.Create(() => LocalSettingsProvider.Instance.SaveAsync().Wait())
				.AddTo(this);

			Disposable.Create(() => Application.Current.TaskTrayIcon.Reload())
				.AddTo(this);
		}

		protected override void InitializeCore()
		{
			base.InitializeCore();
			this._hookService.Suspend()
				.AddTo(this);
		}

		[UsedImplicitly]
		public void OpenBackgroundPathDialog()
		{
			var message = new FolderSelectionMessage("Window.OpenBackgroundImagesDialog.Open")
			{
				Title = Resources.Settings_Background_SelectionDialog,
				SelectedPath = Settings.General.DesktopBackgroundFolderPath,
				DialogPreference = FolderSelectionDialogPreference.CommonItemDialog,
			};
			this.Messenger.Raise(message);

			if (Directory.Exists(message.Response))
			{
				Settings.General.DesktopBackgroundFolderPath.Value = message.Response;
			}
		}
	}

	public class BindableTextViewModel : ViewModel
	{
		#region Text 変更通知プロパティ

		private string _Text;

		public string Text
		{
			get => this._Text;
			set
			{
				if (this._Text != value)
				{
					this._Text = value;
					this.RaisePropertyChanged();
				}
			}
		}

		#endregion
	}

	public class HyperlinkViewModel : BindableTextViewModel
	{
		#region Uri 変更通知プロパティ

		private Uri _Uri;

		public Uri Uri
		{
			get => this._Uri;
			set
			{
				if (this._Uri != value)
				{
					this._Uri = value;
					this.RaisePropertyChanged();
				}
			}
		}

		#endregion
	}

	public class LogViewModel : ViewModel
	{
		#region Header 変更通知プロパティ

		private string _Header;

		public string Header
		{
			get => this._Header;
			set
			{
				if (this._Header != value)
				{
					this._Header = value;
					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

		#region Content 変更通知プロパティ

		private string _Content;

		public string Content
		{
			get => this._Content;
			set
			{
				if (this._Content != value)
				{
					this._Content = value;
					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

		public LogViewModel(ILog log)
		{
			this.Header = $"{log.DateTime:G} {log.Header}";
			this.Content = log.Content;
		}
	}
}

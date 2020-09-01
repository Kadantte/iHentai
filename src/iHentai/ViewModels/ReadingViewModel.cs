﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using iHentai.Data;
using PropertyChanged;

namespace iHentai.ViewModels
{
    internal abstract class ReadingViewModel : ViewModelBase
    {
        public string? Title { get; protected set; }
        public bool IsLoading { get; protected set; }
        private int _selectedIndex;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (Images != null)
                {
                    _selectedIndex = value;
                    OnPropertyChanged(nameof(SelectedIndex));
                }
            }
        }

        public IEnumerable<ReadingImages.IReadingImage>? Images { get; protected set; }

        [DependsOn(nameof(Images))]
        [AlsoNotifyFor(nameof(ViewMode))]
        public int Count => (Images?.Count() ?? 1) - 1;

        public bool CanSwitchChapter { get; protected set; } = true;

        public FlowDirection FlowDirection
        {
            get
            {
                var value = SettingsDb.Instance.Get("reading_flow_direction", FlowDirection.LeftToRight.ToString());
                Enum.TryParse<FlowDirection>(value, out var result);
                return result;
            }
            set
            {
                SettingsDb.Instance.Set("reading_flow_direction", value.ToString());
                OnPropertyChanged(nameof(FlowDirection));
            }
        }

        public ReadingViewMode ViewMode
        {
            get
            {
                if (Images?.Count() == 1)
                {
                    return ReadingViewMode.Flip;
                }
                
                var value = SettingsDb.Instance.Get("reading_mode", ReadingViewMode.Flip.ToString());
                Enum.TryParse<ReadingViewMode>(value, out var result);
                return result;
            }
            set
            {
                SettingsDb.Instance.Set("reading_mode", value.ToString());
                OnPropertyChanged(nameof(ViewMode));
            }
        }

        public bool AutoHideUi
        {
            get => bool.Parse(SettingsDb.Instance.Get("reading_auto_hide_ui", false.ToString()) ?? false.ToString());
            set
            {
                SettingsDb.Instance.Set("reading_auto_hide_ui", value.ToString());
                OnPropertyChanged(nameof(AutoHideUi));
            }
        }


        [DependsOn(nameof(FlowDirection))] public bool IsLTR
        {
            get => FlowDirection == FlowDirection.LeftToRight;
            set => FlowDirection = FlowDirection.LeftToRight;
        }

        [DependsOn(nameof(FlowDirection))] public bool IsRTL
        {
            get => FlowDirection == FlowDirection.RightToLeft;
            set => FlowDirection = FlowDirection.RightToLeft;
        }

        [DependsOn(nameof(ViewMode))] public bool IsBookMode
        {
            get => ViewMode == ReadingViewMode.Book;
            set => ViewMode = ReadingViewMode.Book;
        }

        [DependsOn(nameof(ViewMode))] public bool IsFlipMode
        {
            get => ViewMode == ReadingViewMode.Flip;
            set => ViewMode = ReadingViewMode.Flip;
        }

        [DependsOn(nameof(ViewMode))] public bool IsListMode
        {
            get => ViewMode == ReadingViewMode.List;
            set => ViewMode = ReadingViewMode.List;
        }

        public void ReloadCurrent()
        {
            if (ViewMode == ReadingViewMode.Flip)
            {
                Images?.ElementAtOrDefault(SelectedIndex)?.Reload();
            }
            else if (ViewMode == ReadingViewMode.Book)
            {
                Images?.ElementAtOrDefault(SelectedIndex)?.Reload();
                Images?.ElementAtOrDefault(SelectedIndex + 1)?.Reload();
            }
        }

        public void Next()
        {
            if (Images != null && SelectedIndex < Images.Count() - 1)
            {
                SelectedIndex++;
            }
        }

        public void Previous()
        {
            if (Images != null && SelectedIndex > 0)
            {
                SelectedIndex--;
            }
        }
    }
    
    public enum ReadingViewMode
    {
        Book,
        Flip,
        List
    }
}

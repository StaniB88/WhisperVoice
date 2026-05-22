// Copyright (C) 2026 AnyAutomation
//
// This file is part of WhisperVoice.
//
// WhisperVoice is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// WhisperVoice is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with WhisperVoice.  If not, see <https://www.gnu.org/licenses/>.

using Avalonia.Controls;
using Avalonia.Input;

namespace WhisperVoice.Views;

public partial class SetupWindow : Window
{
    public SetupWindow()
    {
        InitializeComponent();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Allow dragging the window from the titlebar area
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var position = e.GetPosition(TitleBar);
            if (position.Y >= 0 && position.Y <= TitleBar.Bounds.Height &&
                position.X >= 0 && position.X <= TitleBar.Bounds.Width)
            {
                BeginMoveDrag(e);
            }
        }
    }
}

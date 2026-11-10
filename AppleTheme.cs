using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace XioPuzzleStudio
{
    public static class AppleTheme
    {
        public static ResourceDictionary BuildThemeDictionary(bool darkMode)
        {
            // Authentic Apple macOS Sequoia / Cupertino HIG Color Tokens
            string winBg      = darkMode ? "#131315" : "#F5F5F7";
            string chromeBg   = darkMode ? "#1C1C1E" : "#EBEBF0";
            string sidebarBg  = darkMode ? "#18181B" : "#EFEFF4";
            string cardBg     = darkMode ? "#1F1F23" : "#FFFFFF";
            string cardAltBg  = darkMode ? "#28282D" : "#F2F2F7";
            string inputBg    = darkMode ? "#2C2C31" : "#F2F2F7";
            string inputHover = darkMode ? "#36363C" : "#E5E5EA";
            string popupBg    = darkMode ? "#252529" : "#FFFFFF";
            string segTrackBg = darkMode ? "#121214" : "#DDDDE3";
            string segActBg   = darkMode ? "#34343A" : "#FFFFFF";
            string hairline   = darkMode ? "#323238" : "#D8D8DF";
            string textPri    = darkMode ? "#F5F5F7" : "#1D1D1F";
            string textSec    = darkMode ? "#98989F" : "#6E6E73";
            string accent     = darkMode ? "#0A84FF" : "#007AFF";
            string accentHov  = darkMode ? "#409CFF" : "#0062CC";
            string green      = darkMode ? "#30D158" : "#34C759";
            string orange     = darkMode ? "#FF9F0A" : "#FF9500";
            string red        = darkMode ? "#FF453A" : "#FF3B30";
            string purple     = darkMode ? "#BF5AF2" : "#AF52DE";
            string switchOff  = darkMode ? "#39393D" : "#E0E0E6";
            string scrollThmb = darkMode ? "#52525B" : "#AEAEB6";
            string scrollHov  = darkMode ? "#71717A" : "#8E8E93";

            string xaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

  <!-- APPLE COLOR BRUSHES -->
  <SolidColorBrush x:Key='ApWinBg' Color='" + winBg + @"'/>
  <SolidColorBrush x:Key='ApChromeBg' Color='" + chromeBg + @"'/>
  <SolidColorBrush x:Key='ApSidebarBg' Color='" + sidebarBg + @"'/>
  <SolidColorBrush x:Key='ApCardBg' Color='" + cardBg + @"'/>
  <SolidColorBrush x:Key='ApCardAltBg' Color='" + cardAltBg + @"'/>
  <SolidColorBrush x:Key='ApInputBg' Color='" + inputBg + @"'/>
  <SolidColorBrush x:Key='ApInputHover' Color='" + inputHover + @"'/>
  <SolidColorBrush x:Key='ApPopupBg' Color='" + popupBg + @"'/>
  <SolidColorBrush x:Key='ApSegTrack' Color='" + segTrackBg + @"'/>
  <SolidColorBrush x:Key='ApSegActive' Color='" + segActBg + @"'/>
  <SolidColorBrush x:Key='ApHairline' Color='" + hairline + @"'/>
  <SolidColorBrush x:Key='ApTextPri' Color='" + textPri + @"'/>
  <SolidColorBrush x:Key='ApTextSec' Color='" + textSec + @"'/>
  <SolidColorBrush x:Key='ApBlue' Color='" + accent + @"'/>
  <SolidColorBrush x:Key='ApBlueHover' Color='" + accentHov + @"'/>
  <SolidColorBrush x:Key='ApGreen' Color='" + green + @"'/>
  <SolidColorBrush x:Key='ApOrange' Color='" + orange + @"'/>
  <SolidColorBrush x:Key='ApRed' Color='" + red + @"'/>
  <SolidColorBrush x:Key='ApPurple' Color='" + purple + @"'/>
  <SolidColorBrush x:Key='ApSwitchOff' Color='" + switchOff + @"'/>
  <SolidColorBrush x:Key='ApScrollThumb' Color='" + scrollThmb + @"'/>
  <SolidColorBrush x:Key='ApScrollHover' Color='" + scrollHov + @"'/>

  <!-- APPLE PILL SCROLLBARS (BOTH VERTICAL & HORIZONTAL) -->
  <Style x:Key='ApThumbStyle' TargetType='{x:Type Thumb}'>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Thumb}'>
          <Border x:Name='bd' Background='{DynamicResource ApScrollThumb}' CornerRadius='4' Margin='2'/>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='bd' Property='Background' Value='{DynamicResource ApScrollHover}'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='{x:Type ScrollBar}'>
    <Setter Property='Background' Value='Transparent'/>
    <Style.Triggers>
      <Trigger Property='Orientation' Value='Vertical'>
        <Setter Property='Width' Value='11'/>
        <Setter Property='Template'>
          <Setter.Value>
            <ControlTemplate TargetType='{x:Type ScrollBar}'>
              <Grid Background='Transparent' Width='11'>
                <Track Name='PART_Track' IsDirectionReversed='true'>
                  <Track.Thumb><Thumb Style='{StaticResource ApThumbStyle}'/></Track.Thumb>
                </Track>
              </Grid>
            </ControlTemplate>
          </Setter.Value>
        </Setter>
      </Trigger>
      <Trigger Property='Orientation' Value='Horizontal'>
        <Setter Property='Height' Value='11'/>
        <Setter Property='Template'>
          <Setter.Value>
            <ControlTemplate TargetType='{x:Type ScrollBar}'>
              <Grid Background='Transparent' Height='11'>
                <Track Name='PART_Track' IsDirectionReversed='false'>
                  <Track.Thumb><Thumb Style='{StaticResource ApThumbStyle}'/></Track.Thumb>
                </Track>
              </Grid>
            </ControlTemplate>
          </Setter.Value>
        </Setter>
      </Trigger>
    </Style.Triggers>
  </Style>

  <!-- APPLE SCROLLVIEWER (TRANSPARENT CORNER — ZERO WHITE WINDOWS SQUARE) -->
  <Style TargetType='{x:Type ScrollViewer}'>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type ScrollViewer}'>
          <Grid Background='{TemplateBinding Background}'>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width='*'/>
              <ColumnDefinition Width='Auto'/>
            </Grid.ColumnDefinitions>
            <Grid.RowDefinitions>
              <RowDefinition Height='*'/>
              <RowDefinition Height='Auto'/>
            </Grid.RowDefinitions>
            <ScrollContentPresenter x:Name='PART_ScrollContentPresenter'
                                    Grid.Column='0' Grid.Row='0'
                                    Margin='{TemplateBinding Padding}'
                                    Content='{TemplateBinding Content}'
                                    ContentTemplate='{TemplateBinding ContentTemplate}'
                                    CanContentScroll='{TemplateBinding CanContentScroll}'/>
            <ScrollBar x:Name='PART_VerticalScrollBar'
                       Grid.Column='1' Grid.Row='0'
                       Orientation='Vertical'
                       AutomationProperties.AutomationId='VerticalScrollBar'
                       Cursor='Arrow'
                       Maximum='{TemplateBinding ScrollableHeight}'
                       Minimum='0'
                       Value='{Binding VerticalOffset, Mode=OneWay, RelativeSource={RelativeSource TemplatedParent}}'
                       ViewportSize='{TemplateBinding ViewportHeight}'
                       Visibility='{TemplateBinding ComputedVerticalScrollBarVisibility}'/>
            <ScrollBar x:Name='PART_HorizontalScrollBar'
                       Grid.Column='0' Grid.Row='1'
                       Orientation='Horizontal'
                       AutomationProperties.AutomationId='HorizontalScrollBar'
                       Cursor='Arrow'
                       Maximum='{TemplateBinding ScrollableWidth}'
                       Minimum='0'
                       Value='{Binding HorizontalOffset, Mode=OneWay, RelativeSource={RelativeSource TemplatedParent}}'
                       ViewportSize='{TemplateBinding ViewportWidth}'
                       Visibility='{TemplateBinding ComputedHorizontalScrollBarVisibility}'/>
            <Border Grid.Column='1' Grid.Row='1' Background='Transparent'/>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- APPLE SECONDARY BUTTON -->
  <Style x:Key='ApButton' TargetType='{x:Type Button}'>
    <Setter Property='Background' Value='{DynamicResource ApInputBg}'/>
    <Setter Property='Foreground' Value='{DynamicResource ApTextPri}'/>
    <Setter Property='BorderBrush' Value='{DynamicResource ApHairline}'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='Padding' Value='12,6'/>
    <Setter Property='FontSize' Value='12.5'/>
    <Setter Property='FontWeight' Value='Medium'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='SnapsToDevicePixels' Value='True'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='bdr' Background='{TemplateBinding Background}'
                  BorderBrush='{TemplateBinding BorderBrush}'
                  BorderThickness='{TemplateBinding BorderThickness}'
                  CornerRadius='8' Padding='{TemplateBinding Padding}'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='bdr' Property='Background' Value='{DynamicResource ApInputHover}'/>
            </Trigger>
            <Trigger Property='IsPressed' Value='True'>
              <Setter TargetName='bdr' Property='Opacity' Value='0.78'/>
            </Trigger>
            <Trigger Property='IsEnabled' Value='False'>
              <Setter TargetName='bdr' Property='Opacity' Value='0.42'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- APPLE PRIMARY ACCENT BUTTON -->
  <Style x:Key='ApPrimaryButton' TargetType='{x:Type Button}' BasedOn='{StaticResource ApButton}'>
    <Setter Property='Background' Value='{DynamicResource ApBlue}'/>
    <Setter Property='Foreground' Value='#FFFFFF'/>
    <Setter Property='BorderThickness' Value='0'/>
    <Setter Property='FontWeight' Value='SemiBold'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='bdr' Background='{TemplateBinding Background}' CornerRadius='8' Padding='{TemplateBinding Padding}'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='bdr' Property='Opacity' Value='0.88'/>
            </Trigger>
            <Trigger Property='IsPressed' Value='True'>
              <Setter TargetName='bdr' Property='Opacity' Value='0.72'/>
            </Trigger>
            <Trigger Property='IsEnabled' Value='False'>
              <Setter TargetName='bdr' Property='Opacity' Value='0.40'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- APPLE SEGMENTED CONTROL ITEM -->
  <Style x:Key='ApSegmentButton' TargetType='{x:Type Button}'>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Foreground' Value='{DynamicResource ApTextSec}'/>
    <Setter Property='Padding' Value='14,5'/>
    <Setter Property='FontSize' Value='12.5'/>
    <Setter Property='FontWeight' Value='SemiBold'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='bdr' Background='{TemplateBinding Background}' CornerRadius='6' Padding='{TemplateBinding Padding}'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter Property='Foreground' Value='{DynamicResource ApTextPri}'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- APPLE iOS / macOS PILL TOGGLE SWITCH -->
  <Style x:Key='ApToggleSwitch' TargetType='{x:Type CheckBox}'>
    <Setter Property='Width' Value='42'/>
    <Setter Property='Height' Value='24'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type CheckBox}'>
          <Grid Width='42' Height='24'>
            <Border x:Name='track' Background='{DynamicResource ApSwitchOff}' CornerRadius='12'/>
            <Border x:Name='knob' Width='20' Height='20' CornerRadius='10' Background='#FFFFFF'
                    HorizontalAlignment='Left' VerticalAlignment='Center' Margin='2,0,0,0'>
              <Border.Effect>
                <DropShadowEffect BlurRadius='3' ShadowDepth='1' Opacity='0.28'/>
              </Border.Effect>
            </Border>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='IsChecked' Value='True'>
              <Setter TargetName='track' Property='Background' Value='{DynamicResource ApGreen}'/>
              <Setter TargetName='knob' Property='HorizontalAlignment' Value='Right'/>
              <Setter TargetName='knob' Property='Margin' Value='0,0,2,0'/>
            </Trigger>
            <Trigger Property='IsEnabled' Value='False'>
              <Setter TargetName='track' Property='Opacity' Value='0.48'/>
              <Setter TargetName='knob' Property='Opacity' Value='0.75'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- APPLE ROUNDED TEXTBOX -->
  <Style x:Key='ApTextBox' TargetType='{x:Type TextBox}'>
    <Setter Property='Background' Value='{DynamicResource ApInputBg}'/>
    <Setter Property='Foreground' Value='{DynamicResource ApTextPri}'/>
    <Setter Property='CaretBrush' Value='{DynamicResource ApBlue}'/>
    <Setter Property='BorderBrush' Value='{DynamicResource ApHairline}'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='Height' Value='30'/>
    <Setter Property='Padding' Value='9,4'/>
    <Setter Property='FontSize' Value='12.5'/>
    <Setter Property='VerticalContentAlignment' Value='Center'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type TextBox}'>
          <Border x:Name='bdr' Background='{TemplateBinding Background}'
                  BorderBrush='{TemplateBinding BorderBrush}'
                  BorderThickness='{TemplateBinding BorderThickness}'
                  CornerRadius='7'>
            <ScrollViewer x:Name='PART_ContentHost' Margin='2,0' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsKeyboardFocused' Value='True'>
              <Setter TargetName='bdr' Property='BorderBrush' Value='{DynamicResource ApBlue}'/>
              <Setter TargetName='bdr' Property='BorderThickness' Value='1.5'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- APPLE macOS POP-UP COMBOBOX (ZERO WINDOWS CHROME!) -->
  <ControlTemplate x:Key='ApComboToggle' TargetType='{x:Type ToggleButton}'>
    <Border x:Name='bdr' Background='{DynamicResource ApInputBg}'
            BorderBrush='{DynamicResource ApHairline}' BorderThickness='1' CornerRadius='7'>
      <Grid>
        <Grid.ColumnDefinitions>
          <ColumnDefinition Width='*'/>
          <ColumnDefinition Width='24'/>
        </Grid.ColumnDefinitions>
        <Border Grid.Column='1' Width='18' Height='18' CornerRadius='4' Background='{DynamicResource ApBlue}'
                HorizontalAlignment='Center' VerticalAlignment='Center' Margin='0,0,3,0'>
          <Path Data='M 0,2.5 L 3.5,0 L 7,2.5 M 0,5.5 L 3.5,8 L 7,5.5'
                Stroke='#FFFFFF' StrokeThickness='1.3' HorizontalAlignment='Center' VerticalAlignment='Center'/>
        </Border>
      </Grid>
    </Border>
    <ControlTemplate.Triggers>
      <Trigger Property='IsMouseOver' Value='True'>
        <Setter TargetName='bdr' Property='Background' Value='{DynamicResource ApInputHover}'/>
      </Trigger>
      <Trigger Property='IsChecked' Value='True'>
        <Setter TargetName='bdr' Property='BorderBrush' Value='{DynamicResource ApBlue}'/>
      </Trigger>
    </ControlTemplate.Triggers>
  </ControlTemplate>

  <Style x:Key='ApComboItem' TargetType='{x:Type ComboBoxItem}'>
    <Setter Property='Foreground' Value='{DynamicResource ApTextPri}'/>
    <Setter Property='Padding' Value='10,6'/>
    <Setter Property='FontSize' Value='12.5'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type ComboBoxItem}'>
          <Border x:Name='ibd' Background='Transparent' CornerRadius='5' Margin='4,1' Padding='{TemplateBinding Padding}'>
            <ContentPresenter/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsHighlighted' Value='True'>
              <Setter TargetName='ibd' Property='Background' Value='{DynamicResource ApBlue}'/>
              <Setter Property='Foreground' Value='#FFFFFF'/>
            </Trigger>
            <Trigger Property='IsSelected' Value='True'>
              <Setter Property='FontWeight' Value='SemiBold'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='ApComboBox' TargetType='{x:Type ComboBox}'>
    <Setter Property='Foreground' Value='{DynamicResource ApTextPri}'/>
    <Setter Property='Height' Value='30'/>
    <Setter Property='FontSize' Value='12.5'/>
    <Setter Property='ItemContainerStyle' Value='{StaticResource ApComboItem}'/>
    <Setter Property='SnapsToDevicePixels' Value='True'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type ComboBox}'>
          <Grid>
            <ToggleButton Name='ToggleButton' Template='{StaticResource ApComboToggle}'
                          Focusable='false' ClickMode='Press'
                          IsChecked='{Binding Path=IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'/>
            <ContentPresenter Name='ContentSite' IsHitTestVisible='False'
                              Content='{TemplateBinding SelectionBoxItem}'
                              ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'
                              ContentTemplateSelector='{TemplateBinding ItemTemplateSelector}'
                              Margin='10,3,28,3' VerticalAlignment='Center' HorizontalAlignment='Left'/>
            <Popup Name='Popup' Placement='Bottom' IsOpen='{TemplateBinding IsDropDownOpen}'
                   AllowsTransparency='True' Focusable='False' PopupAnimation='Fade'>
              <Grid Name='DropDown' SnapsToDevicePixels='True'
                    MinWidth='{TemplateBinding ActualWidth}' MaxHeight='320'>
                <Border Background='{DynamicResource ApPopupBg}' BorderBrush='{DynamicResource ApHairline}'
                        BorderThickness='1' CornerRadius='9' Margin='0,4,0,0' Padding='2,4'>
                  <ScrollViewer Margin='0' SnapsToDevicePixels='True' VerticalScrollBarVisibility='Auto' HorizontalScrollBarVisibility='Auto'>
                    <StackPanel IsItemsHost='True' KeyboardNavigation.DirectionalNavigation='Contained'/>
                  </ScrollViewer>
                </Border>
              </Grid>
            </Popup>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- APPLE CAPSULE PROGRESS BAR -->
  <Style x:Key='ApProgressBar' TargetType='{x:Type ProgressBar}'>
    <Setter Property='Height' Value='7'/>
    <Setter Property='Background' Value='{DynamicResource ApInputBg}'/>
    <Setter Property='Foreground' Value='{DynamicResource ApBlue}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type ProgressBar}'>
          <Grid x:Name='TemplateRoot' SnapsToDevicePixels='true'>
            <Border Background='{TemplateBinding Background}' CornerRadius='3.5'/>
            <Border x:Name='PART_Track' CornerRadius='3.5'/>
            <Decorator x:Name='PART_Indicator' HorizontalAlignment='Left'>
              <Border Background='{TemplateBinding Foreground}' CornerRadius='3.5'/>
            </Decorator>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- APPLE macOS ROUNDED TERMINAL CONSOLE -->
  <Style x:Key='ApTerminalBox' TargetType='{x:Type TextBox}'>
    <Setter Property='Background' Value='#0B0B0D'/>
    <Setter Property='Foreground' Value='#E4E4E7'/>
    <Setter Property='CaretBrush' Value='#30D158'/>
    <Setter Property='BorderBrush' Value='#27272C'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='FontFamily' Value='Consolas'/>
    <Setter Property='FontSize' Value='12'/>
    <Setter Property='Padding' Value='10,8'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type TextBox}'>
          <Border Background='{TemplateBinding Background}'
                  BorderBrush='{TemplateBinding BorderBrush}'
                  BorderThickness='{TemplateBinding BorderThickness}'
                  CornerRadius='8'>
            <ScrollViewer x:Name='PART_ContentHost' Margin='4'
                          VerticalScrollBarVisibility='Auto'
                          HorizontalScrollBarVisibility='Auto'/>
          </Border>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

</ResourceDictionary>";

            return (ResourceDictionary)XamlReader.Parse(xaml);
        }
    }
}

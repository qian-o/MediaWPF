using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MediaWPF.Effects;

public class Hdr2sdrEffect : ShaderEffect
{
    public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty(nameof(Input), typeof(Hdr2sdrEffect), 0);
    public static readonly DependencyProperty ToneP1Property = DependencyProperty.Register(nameof(ToneP1), typeof(double), typeof(Hdr2sdrEffect), new UIPropertyMetadata(0D, PixelShaderConstantCallback(1)));
    public static readonly DependencyProperty ToneP2Property = DependencyProperty.Register(nameof(ToneP2), typeof(double), typeof(Hdr2sdrEffect), new UIPropertyMetadata(0D, PixelShaderConstantCallback(2)));
    public static readonly DependencyProperty ContrastProperty = DependencyProperty.Register(nameof(Contrast), typeof(double), typeof(Hdr2sdrEffect), new UIPropertyMetadata(0D, PixelShaderConstantCallback(3)));
    public static readonly DependencyProperty BrightnessProperty = DependencyProperty.Register(nameof(Brightness), typeof(double), typeof(Hdr2sdrEffect), new UIPropertyMetadata(0D, PixelShaderConstantCallback(4)));

    public Brush Input
    {
        get
        {
            return (Brush)GetValue(InputProperty);
        }
        set
        {
            SetValue(InputProperty, value);
        }
    }

    public double ToneP1
    {
        get
        {
            return (double)GetValue(ToneP1Property);
        }
        set
        {
            SetValue(ToneP1Property, value);
        }
    }

    public double ToneP2
    {
        get
        {
            return (double)GetValue(ToneP2Property);
        }
        set
        {
            SetValue(ToneP2Property, value);
        }
    }

    public double Contrast
    {
        get
        {
            return (double)GetValue(ContrastProperty);
        }
        set
        {
            SetValue(ContrastProperty, value);
        }
    }

    public double Brightness
    {
        get
        {
            return (double)GetValue(BrightnessProperty);
        }
        set
        {
            SetValue(BrightnessProperty, value);
        }
    }

    public Hdr2sdrEffect()
    {
        PixelShader pixelShader = new()
        {
            UriSource = new Uri("/Effects/Hdr2sdrEffect.ps", UriKind.Relative)
        };
        PixelShader = pixelShader;

        UpdateShaderValue(InputProperty);
        UpdateShaderValue(ToneP1Property);
        UpdateShaderValue(ToneP2Property);
        UpdateShaderValue(ContrastProperty);
        UpdateShaderValue(BrightnessProperty);
    }
}

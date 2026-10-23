using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BloodborneLauncher.Core;
using BloodborneLauncher.Views;
internal static class Program
{
 [STAThread]private static void Main(){
  _=new Application();
  var root=new StackPanel();var label=new TextBlock{FontSize=14,Text="Label"};
  var inherited=new TextBlock{Text="Inherited"};var button=new Button{FontSize=16,Content=inherited};
  root.Children.Add(label);root.Children.Add(button);
  PageTypography.SetEnabled(root,true);PageTypography.SetScale(root,1.3);
  Check(Math.Abs(label.FontSize-18.2)<0.001 && Math.Abs(button.FontSize-20.8)<0.001,"Explicit font sizes scale.");
  Check(Math.Abs(inherited.FontSize-20.8)<0.001,"Content inherits its owner's font without double scaling.");
  PageTypography.SetScale(root,0.7);PageTypography.SetScale(root,1.3);
  Check(Math.Abs(label.FontSize-18.2)<0.001,"Repeated resizing never compounds font sizes.");
  var late=new TextBlock{Text="New binding row",FontSize=10};root.Children.Add(late);
  Check(Math.Abs(late.FontSize-13)<0.001,"Dynamically added rows inherit the current scale.");
  PageTypography.SetEnabled(root,false);Check(label.FontSize==14 && button.FontSize==16,"Disabling the scope restores original sizes.");
  label.FontSize=20;PageTypography.SetEnabled(root,true);
  Check(Math.Abs(label.FontSize-26)<0.001,"Late heading styles replace the base size instead of being lost on resize.");
  var choices=new ChoiceStripPanel();for(int i=0;i<6;i++)choices.Children.Add(new Border{Width=110,Height=36});
  Layout(choices,1000);Check(VisualTreeHelper.GetOffset(choices.Children[5]).Y==0,"Wide windows show one complete choice row.");
  Layout(choices,360);Check(VisualTreeHelper.GetOffset(choices.Children[3]).Y>0,"Narrow windows wrap choices without clipping.");
  Check(choices.DesiredSize.Height>=80,"Wrapped rows retain separate hit areas.");
  Console.WriteLine("PASS: explicit/inherited/dynamic typography and responsive full-width choice strips.");
 }
 private static void Layout(FrameworkElement e,double width){e.Measure(new Size(width,double.PositiveInfinity));e.Arrange(new Rect(0,0,width,e.DesiredSize.Height));e.UpdateLayout();}
 private static void Check(bool value,string message){if(!value)throw new Exception(message);}
}

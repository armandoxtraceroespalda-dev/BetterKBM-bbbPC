using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BloodborneLauncher.Core;
using Microsoft.Win32;
namespace BloodborneLauncher.Views;

/// <summary>First-run game selection and local artwork preparation before constructing the launcher.</summary>
public static class GameAssetSetup
{
    public static async Task<bool> EnsureAsync(LauncherSettings settings)
    {
        string Text(string english,string translated)=>BloodborneLauncher.Core.UiText.T(english);
        var game=GameAssetExtractor.FindGameRoot(GameEnvironment.GameDir(settings));
        while(game==null)
        {
            var picker=new OpenFolderDialog { Title=BloodborneLauncher.Core.UiText.T(Text(BloodborneLauncher.Core.UiText.T("Select your Bloodborne game folder (eboot.bin)"),"Selecciona la carpeta de Bloodborne (eboot.bin)")),Multiselect=false };
            if(picker.ShowDialog()!=true)return false;
            game=GameAssetExtractor.FindGameRoot(picker.FolderName);
            if(game==null)MessageBox.Show(Text(BloodborneLauncher.Core.UiText.T("This folder does not contain the required Bloodborne files. Select the folder containing eboot.bin and dvdroot_ps4/menu."),"Esta carpeta no contiene los archivos necesarios. Selecciona la carpeta que contiene eboot.bin y dvdroot_ps4/menu."),BloodborneLauncher.Core.UiText.T("Bloodborne"),MessageBoxButton.OK,MessageBoxImage.Warning);
        }
        if(settings.Str("game_dir")!=game){settings.Set("game_dir",game);settings.Save();}
        var directory=Path.Combine(AppContext.BaseDirectory,"gui");
        // The earlier local pack is still a valid cache and can be reused without extraction.
        MenuAssets.PrepareDirectory();
        bool cached;
        using(LauncherDiagnostics.Measure("Native asset cache validation")) cached=GameAssetExtractor.IsReady(directory);
        LauncherDiagnostics.Record(cached ? "Native assets: cache hit; extraction skipped." : "Native assets: cache miss; local extraction required.",LogLevel.Info);
        bool soundCached=GameMenuSoundExtractor.IsReady(directory);
        bool haveBank=false;
        try { if(!soundCached)haveBank=GameMenuSoundExtractor.FindBanks(game).Length>0; }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException)
        { LauncherDiagnostics.Record("Menu sound discovery: "+error.Message,LogLevel.Warn); }
        bool needSounds=!soundCached && haveBank;
        LauncherDiagnostics.Record(soundCached ? "Menu sounds: cache hit; decoding skipped." : haveBank ?
            "Menu sounds: extracting local FSB clips in an isolated worker." : "Menu sounds: no main FSB bank available; missing cues remain silent.",LogLevel.Info);
        if(cached && !needSounds)return true;
        bool preparingSounds=cached;
        using var cancel=new CancellationTokenSource();
        var title=new TextBlock { Text=BloodborneLauncher.Core.UiText.T(Text(BloodborneLauncher.Core.UiText.T("Loading…"),"Cargando…")),FontSize=21,Margin=new Thickness(0,0,0,15),Foreground=Brushes.Bisque };
        var status=new TextBlock { Text=preparingSounds ? BloodborneLauncher.Core.UiText.T(Text(BloodborneLauncher.Core.UiText.T("Preparing menu sounds from your game files."),"Preparando los sonidos del menú desde los archivos del juego.")) : BloodborneLauncher.Core.UiText.T(Text(BloodborneLauncher.Core.UiText.T("Preparing menu artwork from your game files."),"Preparando el menú desde los archivos del juego.")),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,15) };
        var bar=new ProgressBar { Minimum=0,Maximum=100,Height=9,Margin=new Thickness(0,0,0,14),Foreground=new SolidColorBrush(Color.FromRgb(167,146,100)) };
        var cancelButton=new Button { Content=BloodborneLauncher.Core.UiText.T(Text(BloodborneLauncher.Core.UiText.T("Cancel"),"Cancelar")),Width=96,HorizontalAlignment=HorizontalAlignment.Right };
        var body=new StackPanel { Margin=new Thickness(26) };body.Children.Add(title);body.Children.Add(status);body.Children.Add(bar);body.Children.Add(cancelButton);
        var window=new Window { FontFamily=LauncherAppearance.DetailFont,Title=BloodborneLauncher.Core.UiText.T("Bloodborne"),Content=BloodborneLauncher.Core.UiText.T(body),Width=510,SizeToContent=SizeToContent.Height,ResizeMode=ResizeMode.NoResize,
            WindowStartupLocation=WindowStartupLocation.CenterScreen,Background=new SolidColorBrush(Color.FromRgb(17,19,20)),Foreground=Brushes.Bisque,ShowInTaskbar=true };
        bool busy=true;cancelButton.Click+=(_,_)=>cancel.Cancel();
        window.Closing+=(_,e)=>{if(busy){e.Cancel=true;cancel.Cancel();status.Text=BloodborneLauncher.Core.UiText.T(Text(BloodborneLauncher.Core.UiText.T("Cancelling…"),"Cancelando…"));cancelButton.IsEnabled=false;}};
        var progress=new Progress<GameAssetExtractor.Step>(step=>{
            bar.Value=preparingSounds ? (cached?0:70)+step.Percent*(cached?1:0.3) : step.Percent*(needSounds?0.7:1);
            string translated=preparingSounds ? step.Percent switch{<=5=>"Leyendo el banco de sonidos",<100=>"Preparando los sonidos del menú",_=>"Listo"} :
                step.Percent switch{<=5=>"Leyendo el fondo",<=30=>"Leyendo los adornos del menú",<=55=>"Preparando la fuente",<=90=>"Comprobando los archivos",_=>"Listo"};
            status.Text=BloodborneLauncher.Core.UiText.T(Text(BloodborneLauncher.Core.UiText.T(step.Message),translated));
        });
        window.Show();
        try
        {
            if(!cached)
                using(LauncherDiagnostics.Measure("Native asset extraction (isolated worker)"))
                    await AssetExtractionProcess.ExtractAsync(game,directory,progress,cancel.Token);
            if(needSounds)
            {
                preparingSounds=true;
                try
                {
                    using(LauncherDiagnostics.Measure("Menu sound extraction (isolated worker)"))
                        await MenuSoundExtractionProcess.ExtractAsync(game,directory,progress,cancel.Token);
                }
                catch(OperationCanceledException){throw;}
                catch(Exception error)
                {
                    // Optional audio must never prevent opening the launcher or starting the game.
                    LauncherDiagnostics.Record("Menu sounds could not be prepared: "+error.GetBaseException().Message,LogLevel.Warn);
                }
            }
            return true;
        }
        catch(OperationCanceledException){return false;}
        catch(Exception e)
        {
            MessageBox.Show(window,Text(BloodborneLauncher.Core.UiText.T("Could not prepare the local menu assets:\n\n"),"No se pudieron preparar los archivos del menú:\n\n")+e.GetBaseException().Message,
                BloodborneLauncher.Core.UiText.T("Bloodborne"),MessageBoxButton.OK,MessageBoxImage.Error);return false;
        }
        finally{busy=false;window.Close();}
    }
}

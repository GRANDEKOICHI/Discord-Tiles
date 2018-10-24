﻿Imports Microsoft.Toolkit.Uwp.UI.Animations
Imports Microsoft.Toolkit.Uwp.UI.Controls
Imports Newtonsoft.Json
Imports Windows.Storage
Imports Windows.Storage.AccessCache
Imports Windows.Storage.Pickers
Imports Windows.UI
Imports Windows.UI.Core
Imports Windows.UI.Xaml.Media.Animation

Module Discord

    Public Async Sub Generar(boolBuscarCarpeta As Boolean)

        Dim recursos As New Resources.ResourceLoader()

        Dim frame As Frame = Window.Current.Content
        Dim pagina As Page = frame.Content

        Dim pr As ProgressRing = pagina.FindName("prTiles")
        pr.Visibility = Visibility.Visible

        Dim botonAñadirCarpetaTexto As TextBlock = pagina.FindName("botonAñadirCarpetaTexto")

        Dim botonCarpetaTexto As TextBlock = pagina.FindName("tbConfigCarpeta")

        Dim gv As GridView = pagina.FindName("gridViewTiles")

        gv.Items.Clear()

        Dim carpeta As StorageFolder = Nothing

        Try
            If boolBuscarCarpeta = True Then
                Dim carpetapicker As New FolderPicker()

                carpetapicker.FileTypeFilter.Add("*")
                carpetapicker.ViewMode = PickerViewMode.List

                carpeta = Await carpetapicker.PickSingleFolderAsync()
            Else
                carpeta = Await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("DiscordCarpeta")
            End If
        Catch ex As Exception

        End Try

        Dim listaJuegos As New List(Of Tile)

        If Not carpeta Is Nothing Then
            Dim carpetasJuegos As IReadOnlyList(Of StorageFolder) = Await carpeta.GetFoldersAsync()

            For Each carpetaJuego As StorageFolder In carpetasJuegos
                Dim ficheros As IReadOnlyList(Of StorageFile) = Await carpetaJuego.GetFilesAsync()

                For Each fichero As StorageFile In ficheros
                    Dim nombreFichero As String = fichero.DisplayName.ToLower

                    If nombreFichero = "application_info" Then
                        Dim datosTexto As String = Await FileIO.ReadTextAsync(fichero)
                        Dim datos As DiscordDatos = JsonConvert.DeserializeObject(Of DiscordDatos)(datosTexto)

                        If Not datos Is Nothing Then
                            Dim titulo As String = datos.Titulo

                            Dim tituloBool As Boolean = False
                            Dim i As Integer = 0
                            While i < listaJuegos.Count
                                If listaJuegos(i).Titulo = titulo Then
                                    tituloBool = True
                                End If
                                i += 1
                            End While

                            Dim idSteam As String = Nothing
                            Dim htmlSteam As String = Await Decompiladores.HttpClient(New Uri("https://store.steampowered.com/search/?term=" + datos.Titulo.Replace(" ", "+")))

                            If Not htmlSteam = Nothing Then
                                Dim temp5, temp6 As String
                                Dim int5, int6 As Integer

                                int5 = htmlSteam.IndexOf("<!-- List Items -->")

                                If Not int5 = -1 Then
                                    temp5 = htmlSteam.Remove(0, int5)

                                    int5 = temp5.IndexOf("<span class=" + ChrW(34) + "title" + ChrW(34) + ">" + datos.Titulo + "</span>")

                                    If Not int5 = -1 Then
                                        temp5 = temp5.Remove(int5, temp5.Length - int5)

                                        int5 = temp5.LastIndexOf("data-ds-appid=")
                                        temp5 = temp5.Remove(0, int5 + 15)

                                        int6 = temp5.IndexOf(ChrW(34))
                                        temp6 = temp5.Remove(int6, temp5.Length - int6)

                                        idSteam = temp6.Trim

                                        Dim imagenAncha As New Uri("http://cdn.edgecast.steamstatic.com/steam/apps/" + idSteam + "/header.jpg", UriKind.RelativeOrAbsolute)
                                        Dim imagenGrande As New Uri("http://cdn.akamai.steamstatic.com/steam/apps/" + idSteam + "/capsule_616x353.jpg", UriKind.RelativeOrAbsolute)

                                        If tituloBool = False Then
                                            Dim juego As New Tile(titulo, datos.IDJuego, "discord:///library/" + datos.IDJuego + "/launch",
                                                                  New Uri("https://cdn.discordapp.com/game-assets/" + datos.IDJuego + "/" + datos.IDIcono + ".png?size=1024"),
                                                                  New Uri("https://cdn.discordapp.com/game-assets/" + datos.IDJuego + "/" + datos.IDIcono + ".png?size=1024"),
                                                                  imagenAncha, imagenGrande)
                                            listaJuegos.Add(juego)
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next
            Next

            If listaJuegos.Count > 0 Then
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("DiscordCarpeta", carpeta)
                botonCarpetaTexto.Text = carpeta.Path
                botonAñadirCarpetaTexto.Text = recursos.GetString("Change")
            End If
        End If

        pr.Visibility = Visibility.Collapsed

        Dim panelNoJuegos As Grid = pagina.FindName("panelAvisoNoJuegos")
        Dim gridSeleccionar As Grid = pagina.FindName("gridSeleccionarJuego")

        If listaJuegos.Count > 0 Then
            panelNoJuegos.Visibility = Visibility.Collapsed
            gridSeleccionar.Visibility = Visibility.Visible

            gv.Visibility = Visibility.Visible

            listaJuegos.Sort(Function(x, y) x.Titulo.CompareTo(y.Titulo))

            gv.Items.Clear()

            For Each juego In listaJuegos
                Dim boton As New Button

                Dim imagen As New ImageEx

                Try
                    imagen.Source = New BitmapImage(juego.ImagenAncha)
                Catch ex As Exception

                End Try

                imagen.IsCacheEnabled = True
                imagen.Stretch = Stretch.UniformToFill
                imagen.Padding = New Thickness(0, 0, 0, 0)

                boton.Tag = juego
                boton.Content = imagen
                boton.Padding = New Thickness(0, 0, 0, 0)
                boton.BorderThickness = New Thickness(1, 1, 1, 1)
                boton.BorderBrush = New SolidColorBrush(Colors.Black)
                boton.Background = New SolidColorBrush(Colors.Transparent)

                Dim tbToolTip As TextBlock = New TextBlock With {
                    .Text = juego.Titulo,
                    .FontSize = 16
                }

                ToolTipService.SetToolTip(boton, tbToolTip)
                ToolTipService.SetPlacement(boton, PlacementMode.Mouse)

                AddHandler boton.Click, AddressOf BotonTile_Click
                AddHandler boton.PointerEntered, AddressOf UsuarioEntraBoton
                AddHandler boton.PointerExited, AddressOf UsuarioSaleBoton

                gv.Items.Add(boton)
            Next

            If boolBuscarCarpeta = True Then
                Toast(listaJuegos.Count.ToString + " " + recursos.GetString("GamesDetected"), Nothing)
            End If
        Else
            panelNoJuegos.Visibility = Visibility.Visible
            gridSeleccionar.Visibility = Visibility.Collapsed

            gv.Visibility = Visibility.Collapsed
        End If

    End Sub

    Private Sub BotonTile_Click(sender As Object, e As RoutedEventArgs)

        Dim frame As Frame = Window.Current.Content
        Dim pagina As Page = frame.Content

        Dim botonJuego As Button = e.OriginalSource
        Dim juego As Tile = botonJuego.Tag

        Dim botonAñadirTile As Button = pagina.FindName("botonAñadirTile")
        botonAñadirTile.Tag = juego

        Dim imagenJuegoSeleccionado As ImageEx = pagina.FindName("imagenJuegoSeleccionado")
        imagenJuegoSeleccionado.Source = New BitmapImage(juego.ImagenAncha)

        Dim tbJuegoSeleccionado As TextBlock = pagina.FindName("tbJuegoSeleccionado")
        tbJuegoSeleccionado.Text = juego.Titulo

        Dim gridAñadir As Grid = pagina.FindName("gridAñadirTile")
        gridAñadir.Visibility = Visibility.Visible

        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("tile", botonJuego)

        Dim animacion As ConnectedAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("tile")

        If Not animacion Is Nothing Then
            animacion.TryStart(gridAñadir)
        End If

        Dim tbTitulo As TextBlock = pagina.FindName("tbTitulo")
        tbTitulo.Text = Package.Current.DisplayName + " (" + Package.Current.Id.Version.Major.ToString + "." + Package.Current.Id.Version.Minor.ToString + "." + Package.Current.Id.Version.Build.ToString + "." + Package.Current.Id.Version.Revision.ToString + ") - " + juego.Titulo

        '---------------------------------------------

        Dim titulo1 As TextBlock = pagina.FindName("tituloTileAnchaEnseñar")
        Dim titulo2 As TextBlock = pagina.FindName("tituloTileAnchaPersonalizar")

        Dim titulo3 As TextBlock = pagina.FindName("tituloTileGrandeEnseñar")
        Dim titulo4 As TextBlock = pagina.FindName("tituloTileGrandePersonalizar")

        titulo1.Text = juego.Titulo
        titulo2.Text = juego.Titulo

        titulo3.Text = juego.Titulo
        titulo4.Text = juego.Titulo

        If Not juego.ImagenPequeña = Nothing Then
            Dim imagenPequeña1 As ImageEx = pagina.FindName("imagenTilePequeñaEnseñar")
            Dim imagenPequeña2 As ImageEx = pagina.FindName("imagenTilePequeñaGenerar")
            Dim imagenPequeña3 As ImageEx = pagina.FindName("imagenTilePequeñaPersonalizar")

            imagenPequeña1.Source = juego.ImagenPequeña
            imagenPequeña2.Source = juego.ImagenPequeña
            imagenPequeña3.Source = juego.ImagenPequeña

            imagenPequeña1.Tag = juego.ImagenPequeña
            imagenPequeña2.Tag = juego.ImagenPequeña
            imagenPequeña3.Tag = juego.ImagenPequeña
        End If

        If Not juego.ImagenMediana = Nothing Then
            Dim imagenMediana1 As ImageEx = pagina.FindName("imagenTileMedianaEnseñar")
            Dim imagenMediana2 As ImageEx = pagina.FindName("imagenTileMedianaGenerar")
            Dim imagenMediana3 As ImageEx = pagina.FindName("imagenTileMedianaPersonalizar")

            imagenMediana1.Source = juego.ImagenMediana
            imagenMediana2.Source = juego.ImagenMediana
            imagenMediana3.Source = juego.ImagenMediana

            imagenMediana1.Tag = juego.ImagenMediana
            imagenMediana2.Tag = juego.ImagenMediana
            imagenMediana3.Tag = juego.ImagenMediana
        End If

        If Not juego.ImagenAncha = Nothing Then
            Dim imagenAncha1 As ImageEx = pagina.FindName("imagenTileAnchaEnseñar")
            Dim imagenAncha2 As ImageEx = pagina.FindName("imagenTileAnchaGenerar")
            Dim imagenAncha3 As ImageEx = pagina.FindName("imagenTileAnchaPersonalizar")

            imagenAncha1.Source = juego.ImagenAncha
            imagenAncha2.Source = juego.ImagenAncha
            imagenAncha3.Source = juego.ImagenAncha

            imagenAncha1.Tag = juego.ImagenAncha
            imagenAncha2.Tag = juego.ImagenAncha
            imagenAncha3.Tag = juego.ImagenAncha
        End If

        If Not juego.ImagenGrande = Nothing Then
            Dim imagenGrande1 As ImageEx = pagina.FindName("imagenTileGrandeEnseñar")
            Dim imagenGrande2 As ImageEx = pagina.FindName("imagenTileGrandeGenerar")
            Dim imagenGrande3 As ImageEx = pagina.FindName("imagenTileGrandePersonalizar")

            imagenGrande1.Source = juego.ImagenGrande
            imagenGrande2.Source = juego.ImagenGrande
            imagenGrande3.Source = juego.ImagenGrande

            imagenGrande1.Tag = juego.ImagenGrande
            imagenGrande2.Tag = juego.ImagenGrande
            imagenGrande3.Tag = juego.ImagenGrande
        End If

    End Sub

    Private Sub UsuarioEntraBoton(sender As Object, e As PointerRoutedEventArgs)

        Dim boton As Button = sender
        Dim imagen As ImageEx = boton.Content

        imagen.Saturation(0).Start()

        Window.Current.CoreWindow.PointerCursor = New CoreCursor(CoreCursorType.Hand, 1)

    End Sub

    Private Sub UsuarioSaleBoton(sender As Object, e As PointerRoutedEventArgs)

        Dim boton As Button = sender
        Dim imagen As ImageEx = boton.Content

        imagen.Saturation(1).Start()

        Window.Current.CoreWindow.PointerCursor = New CoreCursor(CoreCursorType.Arrow, 1)

    End Sub

End Module

Public Class DiscordDatos

    <JsonProperty("name")>
    Public Titulo As String

    <JsonProperty("application_id")>
    Public IDJuego As String

    <JsonProperty("icon_hash")>
    Public IDIcono As String

End Class

# WpfInfiniteBoard
Un contrôle grillé infini et entièrement customisable où l'on peut naviguer et placer des cases


![Wpf Infinite Control](https://user-images.githubusercontent.com/56195432/198040384-bff2c962-032e-438d-868c-1093512b2ef4.gif)


[![NuGet version (WpfInfiniteBoard)](https://img.shields.io/nuget/v/WpfInfiniteBoard.svg?style=flat-square)](https://www.nuget.org/packages/WpfInfiniteBoard)
![Workflow](https://github.com/zonetecde/WpfInfiniteBoard/actions/workflows/dotnet.yml/badge.svg)

## Implémentation :

Dans le code WPF de votre fenêtre :

```html
xmlns:InfiniteBoard="clr-namespace:WpfInfiniteBoard;assembly=WpfInfiniteBoard"
d:Width="1920" d:Height="1080" <!-- Important à précisé, sinon votre fenêtre fera une taille immense dans le designer --> 
 
<InfiniteBoard:InfiniteBoardControl x:Name="InfiniteBoard" />
```

### Quelques propiétés et évènement à connaitre :

**AllowUserToZoom** : L'utilisateur peut-il zoomer dans le contrôle ?

**AllowUserToMoveAround** : L'utilisateur peut-il naviguer dans le contrôle ? (clique sur la molette de la souris)

**AllowUserToPlaceCells** : L'utilisateur peut-il placé des cases avec un clique gauche et en supprimer avec un clique droit ?

**CellSize** : Taille des cases


**BorderThickness** : Épaisseur du quadrillage

![image](https://user-images.githubusercontent.com/56195432/197851905-632dd44c-0468-467a-995c-29fa244120e5.png)


**Foregroud** : Couleur du quadrillage

**Background** : Couleur du contrôle

![image](https://user-images.githubusercontent.com/56195432/197851064-cff248cb-b76d-4297-afae-dbcae3192753.png)


**PlacedCellBorderBrush** : Couleur de la bordure des cases ajoutées 

**PlacedCellBackground** : Couleur des cases ajoutées

![image](https://user-images.githubusercontent.com/56195432/197851203-4aeb1647-1e24-46a4-ad2f-b8de5b5ab0a2.png)


**PlacedCellHaveBorder** : Est-ce que les cases ajoutées ont une bordure ?

![image](https://user-images.githubusercontent.com/56195432/197851455-c1d78a4c-801a-456a-9fa8-94726312ebea.png)


**CellAdded** *(sender, e)* : Évènement se délanchant lorsqu'une case est ajouté, *e* étant la nouvelle case (de type Border)


## Quelques méthode à connaitre :


**InfiniteBoard.PlaceCell(int xFromOrigin, int yFromOrigin)** : Place une cellule aux coordonnées par apport à la case du centre du contrôle de la partie affiché lorsque le contrôle est initialisé.


**InfiniteBoard.EraseCell(int xFromOrigin, int yFromOrigin)** : Enlève une cellule aux coordonnées par apport à la case du centre du contrôle de la partie affiché lorsque le contrôle est initialisé.




**ClearBoard()** : Enlève les cases placé du contrôle

**DoesAnyCellsAlreadyExistHere(int xFromOrigin, int yFromOrigin)** *return bool* : Est-ce que une case est placée à ces coordonnées ?

**ChangeBackgroundAndBorderColor(Brush background, Brush foreground)** : Change la couleur de fond et du quadrillage du contrôle

**GetAllChildren()** : Retourne un dictionnaire de toutes les cases placés 

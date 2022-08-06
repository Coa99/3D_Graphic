using Model;
using PZ3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace PZ3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<long, SubstationEntity> Substations = new Dictionary<long, SubstationEntity>();
        Dictionary<long, LineEntity> Lines = new Dictionary<long, LineEntity>();
        Dictionary<long, NodeEntity> Nodes = new Dictionary<long, NodeEntity>();
        Dictionary<long, SwitchEntity> Switches = new Dictionary<long, SwitchEntity>();
        List<GeometryModel3D> vodovi = new List<GeometryModel3D>();
        Dictionary<GeometryModel3D, LineEntity> vodovi2 = new Dictionary<GeometryModel3D, LineEntity>();
        Dictionary<GeometryModel3D, LineEntity> vodovi3 = new Dictionary<GeometryModel3D, LineEntity>();
        Dictionary<GeometryModel3D, LineEntity> vodovi4 = new Dictionary<GeometryModel3D, LineEntity>();
        Dictionary<GeometryModel3D,long> SviEntiteti = new Dictionary<GeometryModel3D, long>();

        // donji levi ugao mape lat: 45,2325, lon: 19.793909,
        // gornji desni lat: 45,277031, lon: 19.894459. 

        public static Dictionary<PowerEntity, GeometryModel3D> entities = new Dictionary<PowerEntity, GeometryModel3D>();
        public static Dictionary<LineEntity, GeometryModel3D> entities2 = new Dictionary<LineEntity, GeometryModel3D>();

        public static Dictionary<LineEntity, ModelVisual3D> entitiesLines = new Dictionary<LineEntity, ModelVisual3D>();
        double minX = 45.2325, maxX = 45.277031, minY = 19.793909, maxY = 19.894459;


        public double noviX, noviY;
        private Point startCoordinates = new Point();
        private Point diffOffset = new Point();
        private Point startRotation = new Point();
        private int CurrentZoom = 1;
        private int MaxZoom = 18;
        private double mapSize = 2;
        private double squareSize = 0.02;
        private double lineSize = 0.005;
        public Int32Collection IndiciesObjects = new Int32Collection() { 2, 3, 1, 2, 1, 0, 7, 1, 3, 7, 5, 1, 6, 5, 7, 6, 4, 5, 6, 2, 4, 2, 0, 4, 2, 7, 3, 2, 6, 7, 0, 1, 5, 0, 5, 4 };
        GeometryModel3D pogodjeniEntitet = null;
        long lineStartNodeID = -1;
        long lineEndNodeID = -1;
        int lineStartNodeType = -1;
        int lineEndNodeType = -1;

        bool otpornost = false;
        bool dodatni = false;
        bool red = false;

        private bool hiddenEntitiesAndLines = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadDataFromXML();
            CrtajCvorove();
            CrtajVodove();


        }

        public void CrtajCvorove()
        {
            double imageX, imageY;
            int brojacSpratova = 0;

            foreach(SubstationEntity s in Substations.Values)
            {

                imageX = ScaleCoordinates(s.X, maxX, minX);
                imageY = ScaleCoordinates1(s.Y, maxY, minY);

                GeometryModel3D substation = CreateModel(imageX, imageY,0);
                
                while (CheckIfCoordinatesMatch((substation.Geometry as MeshGeometry3D).Positions))
                {
                    brojacSpratova++;
                    substation = CreateModel(imageX, imageY, 0, brojacSpratova * squareSize);
                }


                entities.Add(s, substation);
                Mapa.Children.Add(substation);
                //SviEntiteti.Add(s.ID,substation);
                SviEntiteti.Add(substation, s.ID);

                brojacSpratova = 0; //restart za sledeci substation, a kad zavrsi foreach restartovan ce biti za sledeci Cvor-NODE;
            }

            
            foreach (NodeEntity n in Nodes.Values)
            {
                imageX = ScaleCoordinates(n.X, maxX, minX);
                imageY = ScaleCoordinates1(n.Y, maxY, minY);

                GeometryModel3D node = CreateModel(imageX, imageY,1);

                while (CheckIfCoordinatesMatch((node.Geometry as MeshGeometry3D).Positions))
                {
                    brojacSpratova++;
                    node = CreateModel(imageX, imageY, 1, brojacSpratova * squareSize);
                }

                entities.Add(n, node);
                Mapa.Children.Add(node);
                //SviEntiteti.Add(n.ID,node);
                SviEntiteti.Add(node, n.ID);

                brojacSpratova = 0; //restart za sledeci node, a kad zavrsi foreach restartovan ce biti za sledeci Cvor-SWITCH;
            }

            foreach(SwitchEntity s in Switches.Values)
            {
                imageX = ScaleCoordinates(s.X, maxX, minX);
                imageY = ScaleCoordinates1(s.Y, maxY, minY);

                GeometryModel3D sw = CreateModel(imageX, imageY, 2);

                while (CheckIfCoordinatesMatch((sw.Geometry as MeshGeometry3D).Positions))
                {
                    brojacSpratova++;
                    sw = CreateModel(imageX, imageY, 2, brojacSpratova * squareSize);
                }

                entities.Add(s, sw);
                Mapa.Children.Add(sw);
                //SviEntiteti.Add(s.ID,sw);
                SviEntiteti.Add(sw, s.ID);

                brojacSpratova = 0; //restart za sledeci switch, a kad zavrsi foreach ovaj brojac se nece koristiti :)

            }
        }

        private bool CheckIfCoordinatesMatch(Point3DCollection elementCoord)
        {
            foreach(GeometryModel3D item in SviEntiteti.Keys)
            {
                Point3DCollection koordinate = (item.Geometry as MeshGeometry3D).Positions;
                
                if(koordinate[0].X == elementCoord[0].X && koordinate[0].Y == elementCoord[0].Y && koordinate[0].Z == elementCoord[0].Z)
                {
                    return true;
                }

                
                if (koordinate[0].Z == elementCoord[0].Z)
                {
                    //  kreiranje pravougaonika na osnovu dijagonalnih tacaka tj. max i min vrednosti za X i Y koordinate kocke
                    Rect r1 = new Rect(new Point(elementCoord[0].X,elementCoord[0].Y), new Point(elementCoord[3].X,elementCoord[3].Y));
                    Rect r2 = new Rect(new Point(koordinate[0].X, koordinate[0].Y), new Point(koordinate[3].X, koordinate[3].Y));
                    //provera presecanja Donjih kvadrata kocke, jer ako se oni seku, postoji presecanje, jer su  kocke fiksirane Z koordinatom na mapu.
                    Rect r3 = Rect.Intersect(r1, r2);

                    if (r3 != Rect.Empty)
                        return true;
                }
            }
            return false;
        }

        private GeometryModel3D CreateModel(double imageX, double imageY, int tip, double imageZ = 0)
        {
            //tip - boja za odredjeni cvor
            // 0 substation - red
            // 1 node - green
            // 2 switch - blue

            Point3DCollection pozicije = new Point3DCollection();

            pozicije.Add(new Point3D(imageY, imageX, imageZ));
            pozicije.Add(new Point3D(imageY + squareSize, imageX, imageZ));
            pozicije.Add(new Point3D(imageY, imageX + squareSize, imageZ));
            pozicije.Add(new Point3D(imageY + squareSize, imageX + squareSize, imageZ));
            pozicije.Add(new Point3D(imageY, imageX, imageZ + squareSize));
            pozicije.Add(new Point3D(imageY + squareSize, imageX, imageZ + squareSize));
            pozicije.Add(new Point3D(imageY, imageX + squareSize, imageZ + squareSize));
            pozicije.Add(new Point3D(imageY + squareSize, imageX + squareSize, imageZ + squareSize));

            GeometryModel3D node = new GeometryModel3D();

            if(tip == 0) 
                node.Material = new DiffuseMaterial(Brushes.Black);
            else if (tip ==1)
                node.Material = new DiffuseMaterial(Brushes.Lime);
            else if(tip == 2)
                node.Material = new DiffuseMaterial(Brushes.Blue);

            node.Geometry = new MeshGeometry3D()
            {
                Positions = pozicije,
                TriangleIndices = IndiciesObjects
            };

            return node;
        }

        private void CrtajVodove()
        {
            double imageX, imageY;
            double susedImageX, susedImageY;

            foreach(LineEntity line in Lines.Values)
            {
                GeometryModel3D vod2 = new GeometryModel3D();
                for (int i=0; i< line.Vertices.Count -1; i++)
                {

                    Point3DCollection pozicije = new Point3DCollection();
                    GeometryModel3D vod = new GeometryModel3D();
                    Model3DGroup currentLine = new Model3DGroup();
                    if (line.ConductorMaterial == "Steel")
                    {
                        vod.Material = new DiffuseMaterial(Brushes.Black);
                    }
                    else if (line.ConductorMaterial == "Copper")
                    {
                        vod.Material = new DiffuseMaterial(Brushes.SaddleBrown);
                    }
                    else if (line.ConductorMaterial == "Acsr")
                    {
                        vod.Material = new DiffuseMaterial(Brushes.DarkGray);
                    }
                    else
                    {
                        vod.Material = new DiffuseMaterial(Brushes.Black);
                    }
                    

                    imageX = ScaleCoordinates(line.Vertices[i].X, maxX, minX);
                    imageY = ScaleCoordinates1(line.Vertices[i].Y, maxY, minY);

                    susedImageX = ScaleCoordinates(line.Vertices[i + 1].X, maxX, minX);
                    susedImageY = ScaleCoordinates1(line.Vertices[i + 1].Y, maxY, minY);
                    
                    pozicije.Add(new Point3D(imageY, imageX, 0));
                    pozicije.Add(new Point3D(imageY + lineSize, imageX, 0));
                    pozicije.Add(new Point3D(imageY, imageX + lineSize, 0));
                    pozicije.Add(new Point3D(imageY + lineSize, imageX + lineSize, 0));
                    pozicije.Add(new Point3D(susedImageY, susedImageX, lineSize));
                    pozicije.Add(new Point3D(susedImageY + lineSize, susedImageX, lineSize));
                    pozicije.Add(new Point3D(susedImageY, susedImageX + lineSize, lineSize));
                    pozicije.Add(new Point3D(susedImageY + lineSize, susedImageX + lineSize, lineSize));


                    vod.Geometry = new MeshGeometry3D()
                    {
                        Positions = pozicije,
                        TriangleIndices = IndiciesObjects
                    };


                    

                   
                   // entities.Add(line, vod);
                    Mapa.Children.Add(vod);
                    //SviEntiteti.Add(line.ID, vod);
                    vodovi.Add(vod);
                    vodovi2.Add(vod, line);
                    vodovi3.Add(vod, line);
                    vodovi4.Add(vod, line);
                    SviEntiteti.Add(vod, line.ID);
                   // vod2 = vod;
                }
              //  entities.Add(line, vod2);
            }
        }

        private double ScaleCoordinates(double x, double max, double min)
        {
            double izlaz = 0;

            //izlaz = (x - min) / (max - min)  *(mapSize-squareSize);
            izlaz = (x - min) / ((max - min) * (0.5));

            return izlaz ;
        }

        private double ScaleCoordinates1(double y, double max, double min)
        {
            double izlaz = 0;

            //izlaz = (y - min) / (max - min) * (mapSize-squareSize) ;
            izlaz = (y - min) / ((max - min) * (0.5));

            return izlaz;
        }

        public void LoadDataFromXML()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("Geographic.xml");

            XmlNodeList xmlNodeList;

            xmlNodeList = xmlDocument.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity");

            foreach (XmlNode xmlNode in xmlNodeList)
            {
                SubstationEntity s = new SubstationEntity();
                s.ID = long.Parse(xmlNode.SelectSingleNode("Id").InnerText);
                s.Name = xmlNode.SelectSingleNode("Name").InnerText;
                s.X = double.Parse(xmlNode.SelectSingleNode("X").InnerText);
                s.Y = double.Parse(xmlNode.SelectSingleNode("Y").InnerText);

                ToLatLon(s.X, s.Y, 34, out noviX, out noviY);
                s.X = noviX;
                s.Y = noviY;
                //provera da li je u gornjoj i donjoj granici mape imas u specifikaciji koja su ogranicenja
                if (s.X < minX || s.X > maxX || s.Y < minY || s.Y > maxY)
                    continue;

                Substations.Add(s.ID, s);
            }

            xmlNodeList = xmlDocument.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity");

            foreach (XmlNode xmlNode in xmlNodeList)
            {
                NodeEntity n = new NodeEntity();
                n.ID = long.Parse(xmlNode.SelectSingleNode("Id").InnerText);
                n.Name = xmlNode.SelectSingleNode("Name").InnerText;
                n.X = double.Parse(xmlNode.SelectSingleNode("X").InnerText);
                n.Y = double.Parse(xmlNode.SelectSingleNode("Y").InnerText);

                ToLatLon(n.X, n.Y, 34, out noviX, out noviY);
                n.X = noviX;
                n.Y = noviY;
                //provera da li je u granicama slike koja predstavlja mapu;
                if (n.X < minX || n.X > maxX || n.Y < minY || n.Y > maxY)
                    continue;

                Nodes.Add(n.ID, n);
            }

            xmlNodeList = xmlDocument.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity");

            foreach (XmlNode xmlNode in xmlNodeList)
            {
                SwitchEntity s = new SwitchEntity();
                s.ID = long.Parse(xmlNode.SelectSingleNode("Id").InnerText);
                s.Name = xmlNode.SelectSingleNode("Name").InnerText;
                s.X = double.Parse(xmlNode.SelectSingleNode("X").InnerText);
                s.Y = double.Parse(xmlNode.SelectSingleNode("Y").InnerText);
                s.Status = xmlNode.SelectSingleNode("Status").InnerText;

                ToLatLon(s.X, s.Y, 34, out noviX, out noviY);
                s.X = noviX;
                s.Y = noviY;
                //provera da li je u granicama slike koja predstavlja mapu;
                if (s.X < minX || s.X > maxX || s.Y < minY || s.Y > maxY)
                    continue;

                Switches.Add(s.ID, s);
            }

            xmlNodeList = xmlDocument.DocumentElement.SelectNodes("/NetworkModel/Lines/LineEntity");

            foreach (XmlNode xmlNode in xmlNodeList)
            {
                LineEntity l = new LineEntity();
                l.ID = long.Parse(xmlNode.SelectSingleNode("Id").InnerText);
                l.Name = xmlNode.SelectSingleNode("Name").InnerText;
                l.ConductorMaterial = xmlNode.SelectSingleNode("ConductorMaterial").InnerText;
                l.StartNodeID = long.Parse(xmlNode.SelectSingleNode("FirstEnd").InnerText);
                l.EndNodeID = long.Parse(xmlNode.SelectSingleNode("SecondEnd").InnerText);
                l.Resistance = double.Parse(xmlNode.SelectSingleNode("R").InnerText);

                //provera da li mu se start i end node nalaze u jednom od recnika cvorova. ako se nalaze u jednom od recnika onda treba da se nadju i na mapi
                // ovaj uslov -> contains start && contains end 
                if (!ShouldLineEntityBeOnMap(l))
                    continue;

                //samo ako je uslov ispunjen kreira se lista vertices za vod, da se ne zauzima memorija bzvz
                l.Vertices = new List<PointEntity>();
                int brojacVertices = 0;
                foreach (XmlNode item in xmlNode.ChildNodes[9].ChildNodes)
                {
                    brojacVertices++;

                    PointEntity p = new PointEntity();
                    p.X = double.Parse(item.SelectSingleNode("X").InnerText);
                    p.Y = double.Parse(item.SelectSingleNode("Y").InnerText);

                    ToLatLon(p.X, p.Y, 34, out noviX, out noviY);
                    p.X = noviX;
                    p.Y = noviY;

                    //provera da li je Vertices u granicama slike koja predstavlja mapu;
                    if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
                        continue;

                    l.Vertices.Add(p);
                }

                //samo oni vodovi koji imaju kompletni vertices, glupo ce biti da na mapi ima vod koji na jednom kraju nema nista
                if (l.Vertices.Count == brojacVertices)
                {
                    double cvorX, cvorY;
                    PronadjiCvorIVratiKoordinateZaDodavanjeUVerticiesKodVoda(l.StartNodeID, out cvorX, out cvorY);
                    PointEntity start = new PointEntity() { X = cvorX, Y = cvorY };
                    PronadjiCvorIVratiKoordinateZaDodavanjeUVerticiesKodVoda(l.EndNodeID, out cvorX, out cvorY);
                    PointEntity end = new PointEntity() { X = cvorX, Y = cvorY };

                    l.Vertices.Insert(0, start);
                    l.Vertices.Add(end);
                    Lines.Add(l.ID, l);
                }


            }

        }

        private void PronadjiCvorIVratiKoordinateZaDodavanjeUVerticiesKodVoda(long idCvora, out double x, out double y)
        {
            if (Substations.ContainsKey(idCvora))
            {
                x = Substations[idCvora].X;
                y = Substations[idCvora].Y;
            }
            else if (Switches.ContainsKey(idCvora))
            {
                x = Switches[idCvora].X;
                y = Switches[idCvora].Y;
            }
            else
            {
                //moze else jer si obezbedio da se crta samo vod koji ima pocetne i krajnje cvorove u nekom od recnika
                x = Nodes[idCvora].X;
                y = Nodes[idCvora].Y;
            }
        }

        private bool ShouldLineEntityBeOnMap(LineEntity l)
        {
            if(Substations.ContainsKey(l.StartNodeID) || Nodes.ContainsKey(l.StartNodeID) || Switches.ContainsKey(l.StartNodeID))
            {
                if (Substations.ContainsKey(l.EndNodeID) || Nodes.ContainsKey(l.EndNodeID) || Switches.ContainsKey(l.EndNodeID))
                        return true;
            }
            return false;
        }

        //From UTM to Latitude and longitude in decimal
        public static void ToLatLon(double utmX, double utmY, int zoneUTM, out double latitude, out double longitude)
        {
            bool isNorthHemisphere = true;

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = zoneUTM;
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = utmX - 500000;
            var y = isNorthHemisphere ? utmY : utmY - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
            latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
        }

        public void Scena_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            scena.ReleaseMouseCapture();
        }

        public void Scena_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            scena.CaptureMouse();
            startCoordinates = e.GetPosition(this);
            diffOffset.X = transliranje.OffsetX;
            diffOffset.Y = transliranje.OffsetY;
        }

        public void Scena_MouseMove(object sender, MouseEventArgs e)
        {

            if (scena.IsMouseCaptured)
            {
                Point endCoordinates = e.GetPosition(this);
                double offsetX = endCoordinates.X - startCoordinates.X;
                double offsetY = endCoordinates.Y - startCoordinates.Y;
                double w = this.Width;
                double h = this.Height;
                double translateX = (offsetX * 100) / w;
                double translateY = -(offsetY * 100) / h;
                transliranje.OffsetX = diffOffset.X + (translateX / (100 * skaliranje.ScaleX));
                transliranje.OffsetY = diffOffset.Y + (translateY / (100 * skaliranje.ScaleX));
            }
            else if(e.MiddleButton == MouseButtonState.Pressed)
            {
                Point end = e.GetPosition(this);
                double offsetX = end.X - startRotation.X;
                double offsetY = end.Y - startRotation.Y;

                rotiranje.CenterX = 1;
                rotiranje.CenterY = 1;
                rotiranje.CenterZ = 0;

                offsetX = offsetX > 0 ? 1 : -1;
                offsetY = offsetY > 0 ? 1 : -1;

                //da mapa ne nestane priliko rotacije
                if ((Xosa.Angle + (0.3) * offsetY < 87 && Xosa.Angle + (0.3) * offsetY > -71))
                    Xosa.Angle += (0.3) * offsetY;

                if ((Yosa.Angle + (0.3) * offsetX < 100 && Yosa.Angle + (0.3) * offsetX > -71))
                    Yosa.Angle += (0.3) * offsetX;

                startRotation = end;
                 
            }
            else if (e.MiddleButton == MouseButtonState.Released)
            {
                // da ne bezi kada pomeris mapu i onda odes na skroz drugi kraj i malo samo pomeris mis -> mapa se drasticno zarotira
                // ovo bi trebalo da je fix za taj 'problem'
                startRotation = new Point();
            }
        }

        public void Scena_MouseWheel(object sender, MouseWheelEventArgs e)
        {

            if (e.Delta > 0 && CurrentZoom < MaxZoom)
            {
                skaliranje.ScaleX += 0.1;
                skaliranje.ScaleY += 0.1;
                CurrentZoom++;
            }
            else if(e.Delta <0 && CurrentZoom > - MaxZoom)
            {
                skaliranje.ScaleX -= 0.1;
                skaliranje.ScaleY -= 0.1;
                CurrentZoom--;
            }
        }

        public void VratiBojeEntitetima()
        {
            //tip - boja za odredjeni cvor
            // 0 substation - red
            // 1 node - green
            // 2 switch - blue

            if (lineStartNodeID != -1  && lineEndNodeID != -1)
            {
                if      (lineStartNodeType == 0) SviEntiteti.FirstOrDefault(x => x.Value == lineStartNodeID).Key.Material = new DiffuseMaterial(Brushes.Black); //SviEntiteti[lineStartNodeID].Material = new DiffuseMaterial(Brushes.Black);
                else if (lineStartNodeType == 1) SviEntiteti.FirstOrDefault(x => x.Value == lineStartNodeID).Key.Material = new DiffuseMaterial(Brushes.Lime); //SviEntiteti[lineStartNodeID].Material = new DiffuseMaterial(Brushes.Lime);
                else if (lineStartNodeType == 2) SviEntiteti.FirstOrDefault(x => x.Value == lineStartNodeID).Key.Material = new DiffuseMaterial(Brushes.Blue); //SviEntiteti[lineStartNodeID].Material = new DiffuseMaterial(Brushes.Blue);

                if      (lineEndNodeType == 0) SviEntiteti.FirstOrDefault(x => x.Value == lineEndNodeID).Key.Material = new DiffuseMaterial(Brushes.Black); //SviEntiteti[lineEndNodeID].Material = new DiffuseMaterial(Brushes.Black);
                else if (lineEndNodeType == 1) SviEntiteti.FirstOrDefault(x => x.Value == lineEndNodeID).Key.Material = new DiffuseMaterial(Brushes.Lime); //SviEntiteti[lineEndNodeID].Material = new DiffuseMaterial(Brushes.Lime);
                else if (lineEndNodeType == 2) SviEntiteti.FirstOrDefault(x => x.Value == lineEndNodeID).Key.Material = new DiffuseMaterial(Brushes.Blue); //SviEntiteti[lineEndNodeID].Material = new DiffuseMaterial(Brushes.Blue);
            }
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!dodatni)
            {
                foreach (var obj in vodovi)
                {
                    for (int i = 0; i < Mapa.Children.Count(); i++)
                    {
                        if (Mapa.Children[i] == obj)
                        {
                            ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Transparent);
                        }
                    }

                }
                dodatni = true;
            }
            else
            {
                foreach (var obj in vodovi2.Keys)
                {

                    for (int i = 0; i < Mapa.Children.Count(); i++)
                    {
                        if (Mapa.Children[i] == obj)
                        {
                            if(vodovi2[obj].ConductorMaterial == "Steel")
                            ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Black);
                            if (vodovi2[obj].ConductorMaterial == "Copper")
                                ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.SaddleBrown);
                            if (vodovi2[obj].ConductorMaterial == "Acsr")
                                ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.DarkGray);
                        }
                    }
                }
                dodatni = false;
            }
        }

        private void _funkcionalnost2_Click(object sender, RoutedEventArgs e)
        {
            if (!red)
            {
                foreach (var obj in entities.Keys)
                {

                    if (obj.GetType() == typeof(SwitchEntity))
                    {
                        var obj2 = (SwitchEntity)obj;
                        if (obj2.Status == "Open")
                        {

                            for (int i = 0; i < Mapa.Children.Count(); i++)
                            {
                                if (Mapa.Children[i] == entities[obj2])
                                {
                                    ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Green);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < Mapa.Children.Count(); i++)
                            {
                                if (Mapa.Children[i] == entities[obj2])
                                {
                                    ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Red);
                                }
                            }
                        }
                    }
                }


                red = true;
            }
            else
            {
                foreach (var obj in entities.Keys)
                {

                    if (obj.GetType() == typeof(SwitchEntity))
                    {
                        var obj2 = (SwitchEntity)obj;

                        for (int i = 0; i < Mapa.Children.Count(); i++)
                        {
                            if (Mapa.Children[i] == entities[obj2])
                            {
                                ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Blue);
                            }
                        }

                    }
                }
                red = false;
            }
        }

        private void _funkcionalnost3_Click(object sender, RoutedEventArgs e)
        {
            if (!otpornost) 
            {
                foreach (var obj in vodovi3.Keys)
                {

                    for (int i = 0; i < Mapa.Children.Count(); i++)
                    {
                        if (Mapa.Children[i] == obj)
                        {
                            if (vodovi2[obj].Resistance < 1)
                                ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Red);
                            if (vodovi2[obj].Resistance >= 1 && vodovi2[obj].Resistance < 2)
                                ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Orange);
                            if (vodovi2[obj].Resistance >= 2)
                                ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Yellow);
                        }
                    }
                }
                otpornost = true;
            }
            else
            {
                foreach (var obj in vodovi2.Keys)
                {

                    for (int i = 0; i < Mapa.Children.Count(); i++)
                    {
                        if (Mapa.Children[i] == obj)
                        {
                            if (vodovi2[obj].ConductorMaterial == "Steel")
                                ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Black);
                            if (vodovi2[obj].ConductorMaterial == "Copper")
                                ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.SaddleBrown);
                            if (vodovi2[obj].ConductorMaterial == "Acsr")
                                ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.DarkGray);
                        }
                    }
                }
                otpornost = false;
            }
        }

        public void Scena_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mouseCoordinates = e.GetPosition(this);
            PointHitTestParameters pointparams = new PointHitTestParameters(mouseCoordinates);

            pogodjeniEntitet = null;
            VisualTreeHelper.HitTest(this, null,HTResult ,pointparams);
        }

        private HitTestResultBehavior HTResult(HitTestResult rawresult)
        {
            RayHitTestResult rayResult = rawresult as RayHitTestResult;

            if(rayResult != null)
            {
                VratiBojeEntitetima();

                lineStartNodeID = -1;
                lineEndNodeID = -1;

                if (SviEntiteti.ContainsKey(rayResult.ModelHit as GeometryModel3D))
                {
                    pogodjeniEntitet = (GeometryModel3D)rayResult.ModelHit;
                }
                //ako ne pogodis ni jedan entitet koji se nalazi u sviEntitet, tj. ako kliknes na mapu gde nema ni cvora ni voda
                if (pogodjeniEntitet == null) return HitTestResultBehavior.Stop; 
                //long Id = SviEntiteti.FirstOrDefault(k => k.Value == pogodjeniEntitet).Key;
                long Id = SviEntiteti[pogodjeniEntitet]; //dobijamo id pogodjenog entiteta, id je u recniku vrednost
                ToolTip tt = new ToolTip();
                tt.StaysOpen = false;


                if (Substations.ContainsKey(Id))
                {
                    tt.Content = "Substation\n" + "ID: " + Id + "\nName: " + Substations[Id].Name;
                    tt.IsOpen = true;
                }

                if (Nodes.ContainsKey(Id))
                {
                    tt.Content = "Node\n" + "ID: " + Id + "\nName: " + Nodes[Id].Name;
                    tt.IsOpen = true;
                }

                if (Switches.ContainsKey(Id))
                {
                    tt.Content = "Switch\n" + "ID: " + Id + "\nName: " + Switches[Id].Name;
                    tt.IsOpen = true;
                }

                if (Lines.ContainsKey(Id))
                {
                    LineEntity line = Lines[Id];
                    tt.Content = "Line\n" + "ID: " + Id + "\nName: " + line.Name + "\nStartNode: "+ line.StartNodeID +"\nEndNode: " + line.EndNodeID;
                    tt.IsOpen = true;

                    //SviEntiteti[line.StartNodeID].Material = new DiffuseMaterial(Brushes.Chocolate);
                    //SviEntiteti[line.EndNodeID].Material = new DiffuseMaterial(Brushes.Chocolate);
                    SviEntiteti.FirstOrDefault(x => x.Value == line.StartNodeID).Key.Material = new DiffuseMaterial(Brushes.Chocolate);
                    SviEntiteti.FirstOrDefault(x => x.Value == line.EndNodeID).Key.Material = new DiffuseMaterial(Brushes.Chocolate);

                    lineStartNodeType = GetTypeForEntity(line.StartNodeID);
                    lineEndNodeType = GetTypeForEntity(line.EndNodeID);

                    lineStartNodeID = line.StartNodeID;
                    lineEndNodeID = line.EndNodeID;
                }
                
            }

            return HitTestResultBehavior.Stop;
        }

        private int GetTypeForEntity(long id)
        {
            //tip - boja za odredjeni cvor
            // 0 substation - red
            // 1 node - green
            // 2 switch - blue

            if (Substations.ContainsKey(id))
                return 0;
            else if (Nodes.ContainsKey(id))
                return 1;
            else if (Switches.ContainsKey(id))
                return 2;
            else
                return -1;
        }

        private void _funkcionalnost1_Click(object sender, RoutedEventArgs e)
        {
            if (!hiddenEntitiesAndLines)
            {

                foreach (var line in vodovi4.Keys)
                {

                    foreach (var obj in entities.Keys)
                    {

                        if (obj.GetType() == typeof(SwitchEntity))
                        {
                            var obj2 = (SwitchEntity)obj;
                            if (obj2.ID == vodovi4[line].StartNodeID && obj2.Status == "Open")
                            { 
                                for (int i = 0; i < Mapa.Children.Count(); i++)
                                {
                                    if (Mapa.Children[i] == line)
                                    {
                                        ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Transparent);
                                    }
                                }
                                break;
                            }
                        }
                    }
                    foreach (var obj3 in entities.Keys)
                    {
                        if (obj3.GetType() == typeof(SwitchEntity))
                        {
                            var obj2 = (SwitchEntity)obj3;
                            if (obj2.ID == vodovi4[line].EndNodeID)
                            {
                                for (int i = 0; i < Mapa.Children.Count(); i++)
                                {
                                    if (Mapa.Children[i] == entities[obj3])
                                    {
                                        ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Transparent);
                                    }
                                }
                                break;
                            }
                        }
                        else if (obj3.GetType() == typeof(NodeEntity))
                        {
                            var obj2 = (NodeEntity)obj3;
                            if (obj2.ID == vodovi4[line].EndNodeID)
                            {
                                for (int i = 0; i < Mapa.Children.Count(); i++)
                                {
                                    if (Mapa.Children[i] == entities[obj3])
                                    {
                                        ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Transparent);
                                    }
                                }
                                break;
                            }
                        }
                       else
                        {
                            var obj2 = (SubstationEntity)obj3;
                            if (obj2.ID == vodovi4[line].EndNodeID)
                            {
                                for (int i = 0; i < Mapa.Children.Count(); i++)
                                {
                                    if (Mapa.Children[i] == entities[obj3])
                                    {
                                        ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Transparent);
                                    }
                                }
                                break;
                            }
                        }

                    }

                }

                hiddenEntitiesAndLines = true;
            }
            else
            {
                foreach (var line in vodovi2.Keys)
                {

                    foreach (var obj in entities.Keys)
                    {

                        if (obj.GetType() == typeof(SwitchEntity))
                        {
                            var obj2 = (SwitchEntity)obj;
                            if (obj2.ID == vodovi2[line].StartNodeID && obj2.Status == "Open")
                            {
                                for (int i = 0; i < Mapa.Children.Count(); i++)
                                {
                                    if (Mapa.Children[i] == line)
                                    {
                                        if (vodovi2[line].ConductorMaterial == "Steel")
                                            ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Black);
                                        if (vodovi2[line].ConductorMaterial == "Copper")
                                            ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.SaddleBrown);
                                        if (vodovi2[line].ConductorMaterial == "Acsr")
                                            ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.DarkGray);
                                    }
                                }
                                break;
                            }
                        }
                    }
                    foreach (var obj3 in entities.Keys)
                    {
                        if (obj3.GetType() == typeof(SwitchEntity))
                        {
                            var obj2 = (SwitchEntity)obj3;
                            if (obj2.ID == vodovi2[line].EndNodeID)
                            {
                                for (int i = 0; i < Mapa.Children.Count(); i++)
                                {
                                    if (Mapa.Children[i] == entities[obj3])
                                    {
                                        ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Blue);
                                    }
                                }
                                break;
                            }
                        }
                        else if (obj3.GetType() == typeof(NodeEntity))
                        {
                            var obj2 = (NodeEntity)obj3;
                            if (obj2.ID == vodovi2[line].EndNodeID)
                            {
                                for (int i = 0; i < Mapa.Children.Count(); i++)
                                {
                                    if (Mapa.Children[i] == entities[obj3])
                                    {
                                        ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Lime);
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            var obj2 = (SubstationEntity)obj3;
                            if (obj2.ID == vodovi2[line].EndNodeID)
                            {
                                for (int i = 0; i < Mapa.Children.Count(); i++)
                                {
                                    if (Mapa.Children[i] == entities[obj3])
                                    {
                                        ((GeometryModel3D)Mapa.Children[i]).Material = new DiffuseMaterial(System.Windows.Media.Brushes.Black);
                                    }
                                }
                                break;
                            }
                        }

                    }

                }

                hiddenEntitiesAndLines = false;
            }
        }
    }
}

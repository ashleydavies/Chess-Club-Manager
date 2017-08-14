using System;
using System.Collections.Generic;
using System.Text;
using Chess_Club;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;
using System.Xml.Serialization;
using Windows.UI.Xaml;
using Windows.Storage;

namespace Chess_Club.Models {
    public class Member : Model {
        public int ELO { get; set; }
        public bool Picture { get; set; }
        public int PictureNumber { get; set; }
        public string Forename { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public bool Deleted { get; set; }
        public bool HasLogin { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        
        // ELO property for get/settings _ELO
        //public int ELO { get { return _ELO; } set { _ELO = value; } }
        // Automatic name property for requesting in bindings
        [XmlIgnore()]
        public string Name { get { return Forename + " " + Surname; } }
        // And make a simple property to auto obtain bitmaps
        [XmlIgnore()]
        public BitmapImage Bitmap { get {
            if (Picture) {
                return new BitmapImage(new Uri("ms-appdata:///roaming/MemberPhotos/" + PictureNumber + ".png"));

                //await ApplicationData.Current.RoamingFolder.CreateFolderAsync("MemberPhotos", CreationCollisionOption.OpenIfExists)
            } else {
                // ms-appx is the protocol for accessing the project directory
                return new BitmapImage(new Uri("ms-appx:///Assets/MemberDefaultPicture.png"));
            }
        } }

        // If the player is not new (Has played more than 10 games) we give them a
        //  K factor of 15; otherwise, 25. This helps them adjust into where they should be on the
        //  ranking before diminishing the speed of their ranking change.
        [XmlIgnore()]
        public int K_Factor { get { return Games.Count > 10 ? 15 : 25; } }

        [XmlIgnore()]
        public List<Game> Games { get { return App.db.Games.Where(game => game.WhitePlayerID == ID || game.BlackPlayerID == ID).ToList(); } }
    }
}

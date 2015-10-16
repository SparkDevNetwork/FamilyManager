using System;

namespace FamilyManager
{
    public class Settings
    {
        public const float LogoWidth = 113;
        public const float LogoHeight = 55;
        
        public const string General_IconFont = "Bh";
        public const string General_BoldFont = "OpenSans-Bold";
        public const string General_RegularFont = "OpenSans-Regular";
        public const string General_LightFont = "OpenSans-Light";
        
        public const float StatusBar_SpacerWidth = 25;
        public const float StatusBar_Height = 44;
        public const float StatusBar_Opacity = .80f;

        public const float DarkenOpacity = .80f;

        public const int General_MinSearchLength = 3;

        public const int General_AutoLockTime = 10;

        /// <summary>
        /// The text glyph to use as a symbol when the user doesn't have a photo.
        /// </summary>
        public const string AddPerson_NoPhotoSymbol = "";

        /// <summary>
        /// The size of font to use for the no photo symbol
        /// </summary>
        public const float AddPerson_SymbolFontSize = 48;

        /// <summary>
        /// When we store their profile pic, this is what it's called.
        /// When the HasProfileImage flag is true, we'll load it from this file.
        /// </summary>
        public const string AddPerson_PicName = "userPhoto.jpg";

        public const string AddPerson_Icon_Font_Primary = "FontAwesome";
    }
}


namespace NetEti.CustomControls
{
    /// <summary>
    /// Definiert einen eigenen Klassentyp für
    /// eine einfache String-Message, damit diese vom
    /// InfoController über den Typ gesondert von anderen
    /// Messages behandelt werden kann.
    /// </summary>
    /// <remarks>
    /// File: SplashScreenMessage.cs
    /// Autor: Erik Nagel
    ///
    /// 09.07.2015 Erik Nagel: erstellt
    /// </remarks>
    public class SplashScreenMessage
    {
        /// <summary>
        /// Der Meldungstext.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Konstruktor - übernimmt den Meldungstext.
        /// </summary>
        /// <param name="message">Der Meldungstext.</param>
        public SplashScreenMessage(string message)
        {
            this.Message = message;
        }
    }
}

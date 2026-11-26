using System.Globalization;
using EscalaLivre.Domain.Scheduling;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace EscalaLivre.Infrastructure.ImportExport;

internal static class SchedulePdfExporter
{
    static SchedulePdfExporter() => GlobalFontSettings.FontResolver ??= new ScheduleFontResolver();

    public static byte[] Export(EscalaLivreDocument schedule,string? publicationLabel=null)
    {
        var document=new Document();
        document.Info.Title="Escala da equipe - "+schedule.Company.Name;
        document.Info.Author="EscalaEasy";
        var normal=document.Styles[StyleNames.Normal]!;
        normal.Font.Name="Escala Sans";normal.Font.Size=8.5;
        normal.Font.Color=Colors.DarkSlateGray;
        normal.ParagraphFormat.SpaceAfter=Unit.FromPoint(3);

        var section=document.AddSection();
        section.PageSetup.PageFormat=PageFormat.A4;section.PageSetup.Orientation=Orientation.Landscape;
        section.PageSetup.LeftMargin=section.PageSetup.RightMargin=Unit.FromCentimeter(1.7);
        section.PageSetup.TopMargin=Unit.FromCentimeter(1.8);section.PageSetup.BottomMargin=Unit.FromCentimeter(1.6);
        section.PageSetup.HeaderDistance=section.PageSetup.FooterDistance=Unit.FromCentimeter(.7);

        var title=section.AddParagraph(BreakLongWords(schedule.Company.Name));
        title.Format.Font.Size=17;title.Format.Font.Bold=true;title.Format.SpaceAfter=Unit.FromPoint(7);
        if(publicationLabel is not null)section.AddParagraph(publicationLabel).Format.Font.Bold=true;
        if(schedule.PlanningPeriod is { } period)
            section.AddParagraph($"Planejamento: {period.Start:dd/MM/yyyy} a {period.End:dd/MM/yyyy}");
        var rows=SchedulePdfRows.Build(schedule);
        if(rows.Count==0)section.AddParagraph("Nenhum turno salvo neste período.");
        else
        {
            var table=section.AddTable();table.Borders.Width=.35;table.Borders.Color=Colors.LightGray;
            table.Rows.LeftIndent=Unit.Zero;table.TopPadding=table.BottomPadding=Unit.FromPoint(1.5);
            table.Format.SpaceBefore=table.Format.SpaceAfter=Unit.Zero;
            foreach(double width in new[]{5.0,3.5,6.0,5.0,6.5})table.AddColumn(Unit.FromCentimeter(width));
            var heading=table.AddRow();heading.HeadingFormat=true;heading.Format.Font.Bold=true;
            heading.TopPadding=heading.BottomPadding=Unit.FromPoint(2.5);
            heading.Shading.Color=Color.FromRgb(26,92,75);heading.Format.Font.Color=Colors.White;
            string[] labels=["Funcionário","Função","Dias","Horário","Intervalo"];
            for(int i=0;i<labels.Length;i++)heading.Cells[i].AddParagraph(labels[i]);
            int personIndex=-1;
            foreach(var group in rows)
            {
                if(group.StartsEmployee)personIndex++;
                var row=table.AddRow();
                if(personIndex%2==1)row.Shading.Color=Color.FromRgb(243,247,245);
                // Keep each person's rows together visually instead of striping individual date ranges.
                row.Borders.Top.Width=row.Borders.Bottom.Width=0;
                if(group.StartsEmployee)
                {
                    row.Borders.Top.Width=.8;
                    row.Borders.Top.Color=Color.FromRgb(174,197,187);
                    row.Cells[0].Format.Font.Bold=true;
                }
                string dates=group.From==group.To?group.From.ToString("dd/MM/yyyy"):$"{group.From:dd/MM/yyyy} a {group.To:dd/MM/yyyy}";
                string[] cells=[group.StartsEmployee?group.Name:"",group.StartsRole?group.Role:"",dates,group.Hours,group.Interval];
                for(int i=0;i<cells.Length;i++)row.Cells[i].AddParagraph(BreakLongWords(cells[i],i==1?10:20));
            }
            table.Rows[^1].Borders.Bottom.Width=.35;
        }
        if(rows.Any(row=>row.Hours.Contains("(+")||row.Interval.Contains("(+")))section.AddParagraph("+1d = dia seguinte à entrada do turno.");
        var renderer=new PdfDocumentRenderer{Document=document};renderer.RenderDocument();
        DrawFooters(renderer.PdfDocument);
        /* Full fonts preserve nested composite accents in this PDFsharp release. */
        foreach(var style in new[]{XFontStyleEx.Regular,XFontStyleEx.Bold})
            renderer.PdfDocument.AddCharacters(new XFont("Escala Sans",8.5,style,new XPdfFontOptions(PdfFontEmbedding.EmbedCompleteFontFile))," ");
        using var output=new MemoryStream();renderer.PdfDocument.Save(output,false);return output.ToArray();
    }

    private static void DrawFooters(PdfDocument document)
    {
        var brandColor=XColor.FromArgb(0x16,0x4e,0x63);
        var brandFont=new XFont("Escala Brand",9,XFontStyleEx.Bold,new XPdfFontOptions(PdfFontEmbedding.EmbedCompleteFontFile));
        var pageFont=new XFont("Escala Sans",8.5);
        for(int i=0;i<document.PageCount;i++)
        {
            var page=document.Pages[i];
            using var graphics=XGraphics.FromPdfPage(page,XGraphicsPdfPageOptions.Append);
            string pagination=$"Página {i+1} de {document.PageCount}";
            double pageWidth=graphics.MeasureString(pagination,pageFont).Width;
            double escalaWidth=graphics.MeasureString("Escala",brandFont).Width;
            double nameWidth=escalaWidth+graphics.MeasureString("Easy",brandFont).Width;
            double pageLeft=page.Width.Point-XUnit.FromCentimeter(1.7).Point-pageWidth;
            double top=page.Height.Point-31;
            const double iconSize=18,gap=5;
            double logoLeft=pageLeft-14-iconSize-gap-nameWidth;
            // Reuse the calendar geometry from frontend/public/brand/icon.svg as vector paths.
            var state=graphics.Save();
            graphics.TranslateTransform(logoLeft,top+1);
            graphics.ScaleTransform(iconSize/64);
            var pen=new XPen(brandColor,6){LineJoin=XLineJoin.Round};
            graphics.DrawLine(pen,25,19,39,19);
            var outline=new XGraphicsPath();
            outline.AddLine(15,19,14,19);
            outline.AddBezier(14,19,8.477,19,4,23.477,4,29);
            outline.AddLine(4,29,4,49);
            outline.AddBezier(4,49,4,54.523,8.477,59,14,59);
            outline.AddLine(14,59,50,59);
            outline.AddBezier(50,59,55.523,59,60,54.523,60,49);
            outline.AddLine(60,49,60,45);
            graphics.DrawPath(pen,outline);
            var corner=new XGraphicsPath();
            corner.AddLine(49,19,50,19);
            corner.AddBezier(50,19,55.523,19,60,23.477,60,29);
            corner.AddLine(60,29,60,32);
            graphics.DrawPath(pen,corner);
            var rings=new XPen(brandColor,6){LineCap=XLineCap.Round};
            graphics.DrawLine(rings,20,11,20,22);graphics.DrawLine(rings,44,11,44,22);
            graphics.DrawRoundedRectangle(new XSolidBrush(XColor.FromArgb(0x53,0xb5,0x8a)),18,32,20,20,8,8);
            graphics.Restore(state);
            graphics.DrawString("Escala",brandFont,new XSolidBrush(brandColor),
                new XRect(logoLeft+iconSize+gap,top,escalaWidth,20),XStringFormats.CenterLeft);
            graphics.DrawString("Easy",brandFont,new XSolidBrush(XColor.FromArgb(0x53,0xb5,0x8a)),
                new XRect(logoLeft+iconSize+gap+escalaWidth,top,nameWidth-escalaWidth,20),XStringFormats.CenterLeft);
            graphics.DrawString(pagination,pageFont,XBrushes.DarkSlateGray,
                new XRect(pageLeft,top,pageWidth,20),XStringFormats.CenterLeft);
        }
    }
    // Discretionary breaks keep valid, long unspaced names within their table cells.
    private static string BreakLongWords(string text,int maximumRun=20)
    {
        var output=new System.Text.StringBuilder();int length=0;
        foreach(var rune in text.EnumerateRunes())
        {
            if(System.Text.Rune.IsWhiteSpace(rune))length=0;
            else if(length++==maximumRun){output.Append('\u00ad');length=1;}
            output.Append(rune.ToString());
        }
        return output.ToString();
    }
    private sealed class ScheduleFontResolver:IFontResolver
    {
        private static readonly byte[] Regular=Read("NotoSans-Regular.ttf"),Bold=Read("NotoSans-Bold.ttf"),Brand=Read("Inter-Bold.ttf");
        public FontResolverInfo ResolveTypeface(string familyName,bool bold,bool italic)=>new(familyName=="Escala Brand"?"Escala.Brand":bold?"Escala.Bold":"Escala.Regular",false,italic);
        public byte[] GetFont(string faceName)=>faceName=="Escala.Brand"?Brand:faceName=="Escala.Bold"?Bold:Regular;
        private static byte[] Read(string filename)
        {
            using var stream=typeof(SchedulePdfExporter).Assembly.GetManifestResourceStream("EscalaLivre.Fonts."+filename)
                ?? throw new InvalidOperationException("Fonte incorporada ausente: "+filename);
            using var bytes=new MemoryStream();stream.CopyTo(bytes);return bytes.ToArray();
        }
    }
}

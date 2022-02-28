﻿/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  05/16/2020         EPPlus Software AB           ExcelTable Html Export
 *************************************************************************************************/
using OfficeOpenXml.Core.CellStore;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Table;
using OfficeOpenXml.Utils;
using System.IO;
#if !NET35 && !NET40
using System.Threading.Tasks;
#endif

namespace OfficeOpenXml.Export.HtmlExport
{
    /// <summary>
    /// Exports a <see cref="ExcelTable"/> to Html
    /// </summary>
    public partial class ExcelHtmlRangeExporter
    {        
        /// <summary>
        /// Exports an <see cref="ExcelTable"/> to a html string
        /// </summary>
        /// <returns>A html table</returns>
        public string GetCssString()
        {
            using (var ms = RecyclableMemory.GetStream())
            {
                RenderCss(ms);
                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        public void RenderCss(Stream stream)
        {
            if (!stream.CanWrite)
            {
                throw new IOException("Parameter stream must be a writable System.IO.Stream");
            }

            if (_datatypes.Count == 0) GetDataTypes();
            var sw = new StreamWriter(stream);
            RenderCellCss(sw);
        }

        private void RenderCellCss(StreamWriter sw)
        {            
            var styleWriter = new EpplusCssWriter(sw, _range, Settings, Settings.Css, Settings.Css.CssExclude);
            
            styleWriter.RenderAdditionalAndFontCss(TableClass);
            var ws = _range.Worksheet;
            var styles = ws.Workbook.Styles;
            var ce = new CellStoreEnumerator<ExcelValue>(_range.Worksheet._values, _range._fromRow, _range._fromCol, _range._toRow, _range._toCol);
            ExcelAddressBase address = null;
            while (ce.Next())
            {
                if (ce.Value._styleId > 0 && ce.Value._styleId < styles.CellXfs.Count)
                {
                    var ma = ws.MergedCells[ce.Row, ce.Column];
                    if(ma!=null)
                    {
                        if (address == null || address.Address != ma)
                        {
                            address = new ExcelAddressBase(ma);
                        }
                        var fromRow = address._fromRow < _range._fromRow ? _range._fromRow : address._fromRow;
                        var fromCol = address._fromCol < _range._fromCol ? _range._fromCol : address._fromCol;
                        if (fromRow != ce.Row || fromCol != ce.Column) //Only add the style for the top-left cell in the merged range.
                            continue;                        
                    }
                    styleWriter.AddToCss(styles, ce.Value._styleId, Settings.StyleClassPrefix);
                }
            }
            if(Settings.IncludePictures)
            {
                LoadRangeImages(_range);
                foreach(var p in _rangePictures)
                {
                    styleWriter.AddPictureToCss(p);
                }
            }
            if(Settings.IncludePictures) RenderPictures(styleWriter);
            styleWriter.FlushStream();
        }

        private void RenderPictures(EpplusCssWriter styleWriter)
        {
            foreach(ExcelDrawing d in _range.Worksheet.Drawings)
            {
                if(d is ExcelPicture p)
                {
                    var addr = new ExcelAddressBase(p.From.Row, p.From.Column, p.To.Row, p.To.Column);
                    if (_range.Collide(addr) == ExcelAddressBase.eAddressCollition.Inside)
                    {
                        styleWriter.AddPictureToCss(p, addr);
                    }
                }
            }
        }
    }
}

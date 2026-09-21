using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length > 0)
        {
            ProcessFiles(args.Where(File.Exists)
                .Where(x => string.Equals(Path.GetExtension(x), ".pdf", StringComparison.OrdinalIgnoreCase))
                .ToArray());
            return;
        }
        Application.Run(new MainForm());
    }

    static void ProcessFiles(string[] files)
    {
        int created = 0, processed = 0;
        foreach (var file in files)
        {
            try
            {
                using var input = PdfReader.Open(file, PdfDocumentOpenMode.Import);
                if (input.PageCount == 0) continue;

                string dir = Path.GetDirectoryName(file)!;
                string stem = Path.GetFileNameWithoutExtension(file);
                string outputDir = Path.Combine(dir, "Split_Output", stem);
                Directory.CreateDirectory(outputDir);

                for (int start = 0; start < input.PageCount; start += 2)
                {
                    using var output = new PdfDocument();
                    output.AddPage(input.Pages[start]);
                    if (start + 1 < input.PageCount)
                        output.AddPage(input.Pages[start + 1]);

                    int first = start + 1;
                    int last = Math.Min(start + 2, input.PageCount);
                    output.Save(Path.Combine(outputDir,
                        $"{stem}_{first:000}-{last:000}.pdf"));
                    created++;
                }
                processed++;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در پردازش:\n{file}\n\n{ex.Message}",
                    "PDF Splitter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        MessageBox.Show($"انجام شد.\n\nPDF پردازش‌شده: {processed}\nفایل ساخته‌شده: {created}\n\nخروجی‌ها در Split_Output قرار گرفتند.",
            "PDF Splitter", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    sealed class MainForm : Form
    {
        public MainForm()
        {
            Text = "PDF Splitter";
            Width = 620; Height = 330;
            StartPosition = FormStartPosition.CenterScreen;
            AllowDrop = true;

            Controls.Add(new Label {
                Dock = DockStyle.Fill,
                Text = "PDFها را اینجا بکش و رها کن\n\nهر ۲ صفحه → یک PDF جدا\n\nمثلاً ۵۰ صفحه → ۲۵ فایل",
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 16)
            });

            DragEnter += (s,e) => {
                e.Effect = e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)
                    ? DragDropEffects.Copy : DragDropEffects.None;
            };
            DragDrop += (s,e) => {
                var paths = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
                ProcessFiles(paths.Where(File.Exists)
                    .Where(x => string.Equals(Path.GetExtension(x), ".pdf", StringComparison.OrdinalIgnoreCase))
                    .ToArray());
            };
        }
    }
}
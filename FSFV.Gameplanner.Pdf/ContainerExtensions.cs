using QuestPDF.Elements.Table;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FSFV.Gameplanner.Pdf;

static class ContainerExtensions
{

    public static PageDescriptor PlanPageStyle(this PageDescriptor page)
    {
        page.Size(PageSizes.A4.Landscape());
        page.DefaultTextStyle(style => style.FontSize(10));
        return page;
    }

    public static IContainer PlanPageContentStyle(this IContainer content)
    {
        return content
            .AlignCenter()
            .PaddingHorizontal(100)
            .PaddingTop(20)
            .ScaleToFit()
            ;
    }

    private static IContainer PlanCell(this IContainer container)
    {
        return container
            .Border(0)
            .ScaleToFit()
            .PaddingVertical(4)
            .PaddingLeft(5)
            ;
    }

    // displays only text label
    public static void LabelCell(this IContainer container, string text)
        => container.PlanCell().Text(text).Medium();

    // allows you to inject any type of content, e.g. image
    public static IContainer ValueCell(this IContainer container)
        => container.PlanCell();

    public static IContainer RowContainer(this ITableCellContainer container, uint row, Color bgColor)
        => container
                    .Row(row)
                    .ColumnSpan(7)
                    .BorderTop(0.1f)
                    .Background(bgColor)
                    .PlanCell()
        ;
}

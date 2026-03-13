### Standard Emphasis

*This text will be italic*

_Italic using underscores_

**This text will be bold**

__Bold using underscores__

***This text will be both bold and italic***

~~Strikethrough~~

***~~Combined~~* ~~strikethrough and italic~~**

---

### Extended Emphasis (Not defined by default, you can add it in styles)

This is ~subscript~ and this is ^superscript^.

For example, H~2~O is rendered as H₂O and E=mc^2^ is rendered as E=mc².

This has ++underline++ and this has ==highlight==.

---

### Style example

```xaml
<Style Selector="md|MarkdownRenderer">
  <Style Selector="^ Span.Emphasis">
    <Style Selector="^.Subscript">
      <Setter Property="BaselineAlignment" Value="Subscript"/>
      <Setter Property="FontSize" Value="8"/>
    </Style>
    <Style Selector="^.Superscript">
      <Setter Property="BaselineAlignment" Value="Superscript"/>
      <Setter Property="FontSize" Value="8"/>
    </Style>
    <Style Selector="^.Underline">
      <Setter Property="TextDecorations" Value="Underline"/>
    </Style>
    <Style Selector="^.Highlight">
      <Setter Property="Background" Value="DarkOrange"/>
    </Style>
  </Style>
</Style>
```
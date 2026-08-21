<?xml version="1.0" encoding="UTF-8"?>
<!--
  Nokta katmaninin sunucu tarafi stili (WMS gosterimi icin).

  Renk sabit degil: <ogc:PropertyName>color</ogc:PropertyName> ile her
  kaydin kendi color kolonu okunuyor. Boylece WMS'in urettigi resim,
  istemcinin vektor katmaninda cizdigi goruntuyle ayni oluyor.
-->
<StyledLayerDescriptor version="1.0.0"
    xmlns="http://www.opengis.net/sld"
    xmlns:ogc="http://www.opengis.net/ogc"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd">
  <NamedLayer>
    <Name>mapproject_point</Name>
    <UserStyle>
      <Title>MapProject nokta</Title>
      <FeatureTypeStyle>
        <Rule>
          <PointSymbolizer>
            <Graphic>
              <Mark>
                <WellKnownName>circle</WellKnownName>
                <Fill>
                  <CssParameter name="fill"><ogc:PropertyName>color</ogc:PropertyName></CssParameter>
                </Fill>
                <Stroke>
                  <CssParameter name="stroke">#ffffff</CssParameter>
                  <CssParameter name="stroke-width">2</CssParameter>
                </Stroke>
              </Mark>
              <Size>12</Size>
            </Graphic>
          </PointSymbolizer>
          <TextSymbolizer>
            <Label><ogc:PropertyName>name</ogc:PropertyName></Label>
            <Font>
              <CssParameter name="font-family">SansSerif</CssParameter>
              <CssParameter name="font-size">12</CssParameter>
              <CssParameter name="font-weight">bold</CssParameter>
            </Font>
            <LabelPlacement>
              <PointPlacement>
                <AnchorPoint><AnchorPointX>0.5</AnchorPointX><AnchorPointY>0.0</AnchorPointY></AnchorPoint>
                <Displacement><DisplacementX>0</DisplacementX><DisplacementY>10</DisplacementY></Displacement>
              </PointPlacement>
            </LabelPlacement>
            <!-- Beyaz kontur: etiket koyu haritada da okunabilsin. -->
            <Halo>
              <Radius>2</Radius>
              <Fill><CssParameter name="fill">#ffffff</CssParameter></Fill>
            </Halo>
            <Fill><CssParameter name="fill">#0f172a</CssParameter></Fill>
          </TextSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>

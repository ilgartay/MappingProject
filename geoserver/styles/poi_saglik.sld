<?xml version="1.0" encoding="UTF-8"?>
<!--
  Sağlık kategorisindeki POI'ler: kırmızı artı.

  Filtre kategori_yolu uzerinden calisiyor ("Yeme-Icme > Restoran" gibi),
  boylece alt kategoriler de ust kategorisinin gorunumunu aliyor.

  Ikinci kural etiketi ciziyor ve MaxScaleDenominator ile yalnizca harita
  yakinlastirilmisken devreye giriyor - Turkiye geneli goruntude yuzlerce
  isim ust uste binerdi.
-->
<StyledLayerDescriptor version="1.0.0"
    xmlns="http://www.opengis.net/sld"
    xmlns:ogc="http://www.opengis.net/ogc"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd">
  <NamedLayer>
    <Name>poi_saglik</Name>
    <UserStyle>
      <Title>POI - Sağlık</Title>
      <FeatureTypeStyle>
        <Rule>
          <Name>isaret</Name>
          <ogc:Filter>
          <ogc:PropertyIsLike wildCard="*" singleChar="." escapeChar="!">
            <ogc:PropertyName>kategori_yolu</ogc:PropertyName>
            <ogc:Literal>Sağlık*</ogc:Literal>
          </ogc:PropertyIsLike>
          </ogc:Filter>
          <PointSymbolizer>
            <Graphic>
              <Mark>
                <WellKnownName>cross</WellKnownName>
                <Fill><CssParameter name="fill">#dc2626</CssParameter></Fill>
                <Stroke>
                  <CssParameter name="stroke">#ffffff</CssParameter>
                  <CssParameter name="stroke-width">2</CssParameter>
                </Stroke>
              </Mark>
              <Size>14</Size>
            </Graphic>
          </PointSymbolizer>
        </Rule>
        <Rule>
          <Name>isim-etiketi</Name>
          <ogc:Filter>
          <ogc:PropertyIsLike wildCard="*" singleChar="." escapeChar="!">
            <ogc:PropertyName>kategori_yolu</ogc:PropertyName>
            <ogc:Literal>Sağlık*</ogc:Literal>
          </ogc:PropertyIsLike>
          </ogc:Filter>
          <MaxScaleDenominator>2000000</MaxScaleDenominator>
          <TextSymbolizer>
            <Label><ogc:PropertyName>isim</ogc:PropertyName></Label>
            <Font>
              <CssParameter name="font-family">SansSerif</CssParameter>
              <CssParameter name="font-size">12</CssParameter>
              <CssParameter name="font-weight">bold</CssParameter>
            </Font>
            <LabelPlacement>
              <PointPlacement>
                <AnchorPoint><AnchorPointX>0.5</AnchorPointX><AnchorPointY>0.0</AnchorPointY></AnchorPoint>
                <Displacement><DisplacementX>0</DisplacementX><DisplacementY>12</DisplacementY></Displacement>
              </PointPlacement>
            </LabelPlacement>
            <Halo>
              <Radius>2</Radius>
              <Fill><CssParameter name="fill">#ffffff</CssParameter></Fill>
            </Halo>
            <Fill><CssParameter name="fill">#dc2626</CssParameter></Fill>
            <VendorOption name="conflictResolution">true</VendorOption>
          </TextSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>

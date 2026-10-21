# Chemistry — fundamentos de reações e composição de matéria

**Natureza:** documentação científica, não projeto C# independente.

## Fenômenos necessários no primeiro mundo

- Matéria orgânica e detritos com estoques definidos e transferências auditadas.
- Nitrogênio agregado no solo e na biomassa, com absorção, retorno por detritos e mineralização simplificada.
- Taxas de transformação dependentes de umidade/temperatura quando cientificamente justificadas.
- Composição, concentração e balanços de massa somente para espécies químicas efetivamente representadas.
- Conversão explícita entre massa úmida, seca e de nutriente; não usar estas unidades indistintamente.

## Propriedade

`Game.Modules.Geosphere/Soil` é dono dos estoques de fertilidade/nutrientes do solo. `Game.Modules.Biosphere` é dono de biomassa, resíduos e detritos biológicos; a transferência de nutrientes mineralizados para o solo deve ser confirmada por contratos e livro de fluxos. A simplificação de decompositores pertence a `Biosphere/Ecology` e não exige microbiologia individualizada.

## Limites

O MVP **não** implementa química atmosférica, especiação completa de N/P/K, redes bioquímicas ou geoquímica detalhada. Não inventar concentrações ou parâmetros calibrados. Registre o que não existe no modelo.

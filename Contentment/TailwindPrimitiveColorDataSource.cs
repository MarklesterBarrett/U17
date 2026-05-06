using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Community.Contentment.DataEditors;

namespace Site.Contentment;

public sealed class BuiltInBaseColorDataSource : IContentmentDataSource
{
    private static readonly IReadOnlyList<BaseColorDefinition> Colors =
    [
        new("Red 50", "red-50", "oklch(97.1% 0.013 17.38)"),
        new("Red 100", "red-100", "oklch(93.6% 0.032 17.717)"),
        new("Red 200", "red-200", "oklch(88.5% 0.062 18.334)"),
        new("Red 300", "red-300", "oklch(80.8% 0.114 19.571)"),
        new("Red 400", "red-400", "oklch(70.4% 0.191 22.216)"),
        new("Red 500", "red-500", "oklch(63.7% 0.237 25.331)"),
        new("Red 600", "red-600", "oklch(57.7% 0.245 27.325)"),
        new("Red 700", "red-700", "oklch(50.5% 0.213 27.518)"),
        new("Red 800", "red-800", "oklch(44.4% 0.177 26.899)"),
        new("Red 900", "red-900", "oklch(39.6% 0.141 25.723)"),
        new("Red 950", "red-950", "oklch(25.8% 0.092 26.042)"),
        new("Orange 50", "orange-50", "oklch(98% 0.016 73.684)"),
        new("Orange 100", "orange-100", "oklch(95.4% 0.038 75.164)"),
        new("Orange 200", "orange-200", "oklch(90.1% 0.076 70.697)"),
        new("Orange 300", "orange-300", "oklch(83.7% 0.128 66.29)"),
        new("Orange 400", "orange-400", "oklch(75% 0.183 55.934)"),
        new("Orange 500", "orange-500", "oklch(70.5% 0.213 47.604)"),
        new("Orange 600", "orange-600", "oklch(64.6% 0.222 41.116)"),
        new("Orange 700", "orange-700", "oklch(55.3% 0.195 38.402)"),
        new("Orange 800", "orange-800", "oklch(47% 0.157 37.304)"),
        new("Orange 900", "orange-900", "oklch(40.8% 0.123 38.172)"),
        new("Orange 950", "orange-950", "oklch(26.6% 0.079 36.259)"),
        new("Amber 50", "amber-50", "oklch(98.7% 0.022 95.277)"),
        new("Amber 100", "amber-100", "oklch(96.2% 0.059 95.617)"),
        new("Amber 200", "amber-200", "oklch(92.4% 0.12 95.746)"),
        new("Amber 300", "amber-300", "oklch(87.9% 0.169 91.605)"),
        new("Amber 400", "amber-400", "oklch(82.8% 0.189 84.429)"),
        new("Amber 500", "amber-500", "oklch(76.9% 0.188 70.08)"),
        new("Amber 600", "amber-600", "oklch(66.6% 0.179 58.318)"),
        new("Amber 700", "amber-700", "oklch(55.5% 0.163 48.998)"),
        new("Amber 800", "amber-800", "oklch(47.3% 0.137 46.201)"),
        new("Amber 900", "amber-900", "oklch(41.4% 0.112 45.904)"),
        new("Amber 950", "amber-950", "oklch(27.9% 0.077 45.635)"),
        new("Yellow 50", "yellow-50", "oklch(98.7% 0.026 102.212)"),
        new("Yellow 100", "yellow-100", "oklch(97.3% 0.071 103.193)"),
        new("Yellow 200", "yellow-200", "oklch(94.5% 0.129 101.54)"),
        new("Yellow 300", "yellow-300", "oklch(90.5% 0.182 98.111)"),
        new("Yellow 400", "yellow-400", "oklch(85.2% 0.199 91.936)"),
        new("Yellow 500", "yellow-500", "oklch(79.5% 0.184 86.047)"),
        new("Yellow 600", "yellow-600", "oklch(68.1% 0.162 75.834)"),
        new("Yellow 700", "yellow-700", "oklch(55.4% 0.135 66.442)"),
        new("Yellow 800", "yellow-800", "oklch(47.6% 0.114 61.907)"),
        new("Yellow 900", "yellow-900", "oklch(42.1% 0.095 57.708)"),
        new("Yellow 950", "yellow-950", "oklch(28.6% 0.066 53.813)"),
        new("Lime 50", "lime-50", "oklch(98.6% 0.031 120.757)"),
        new("Lime 100", "lime-100", "oklch(96.7% 0.067 122.328)"),
        new("Lime 200", "lime-200", "oklch(93.8% 0.127 124.321)"),
        new("Lime 300", "lime-300", "oklch(89.7% 0.196 126.665)"),
        new("Lime 400", "lime-400", "oklch(84.1% 0.238 128.85)"),
        new("Lime 500", "lime-500", "oklch(76.8% 0.233 130.85)"),
        new("Lime 600", "lime-600", "oklch(64.8% 0.2 131.684)"),
        new("Lime 700", "lime-700", "oklch(53.2% 0.157 131.589)"),
        new("Lime 800", "lime-800", "oklch(45.3% 0.124 130.933)"),
        new("Lime 900", "lime-900", "oklch(40.5% 0.101 131.063)"),
        new("Lime 950", "lime-950", "oklch(27.4% 0.072 132.109)"),
        new("Green 50", "green-50", "oklch(98.2% 0.018 155.826)"),
        new("Green 100", "green-100", "oklch(96.2% 0.044 156.743)"),
        new("Green 200", "green-200", "oklch(92.5% 0.084 155.995)"),
        new("Green 300", "green-300", "oklch(87.1% 0.15 154.449)"),
        new("Green 400", "green-400", "oklch(79.2% 0.209 151.711)"),
        new("Green 500", "green-500", "oklch(72.3% 0.219 149.579)"),
        new("Green 600", "green-600", "oklch(62.7% 0.194 149.214)"),
        new("Green 700", "green-700", "oklch(52.7% 0.154 150.069)"),
        new("Green 800", "green-800", "oklch(44.8% 0.119 151.328)"),
        new("Green 900", "green-900", "oklch(39.3% 0.095 152.535)"),
        new("Green 950", "green-950", "oklch(26.6% 0.065 152.934)"),
        new("Emerald 50", "emerald-50", "oklch(97.9% 0.021 166.113)"),
        new("Emerald 100", "emerald-100", "oklch(95% 0.052 163.051)"),
        new("Emerald 200", "emerald-200", "oklch(90.5% 0.093 164.15)"),
        new("Emerald 300", "emerald-300", "oklch(84.5% 0.143 164.978)"),
        new("Emerald 400", "emerald-400", "oklch(76.5% 0.177 163.223)"),
        new("Emerald 500", "emerald-500", "oklch(69.6% 0.17 162.48)"),
        new("Emerald 600", "emerald-600", "oklch(59.6% 0.145 163.225)"),
        new("Emerald 700", "emerald-700", "oklch(50.8% 0.118 165.612)"),
        new("Emerald 800", "emerald-800", "oklch(43.2% 0.095 166.913)"),
        new("Emerald 900", "emerald-900", "oklch(37.8% 0.077 168.94)"),
        new("Emerald 950", "emerald-950", "oklch(26.2% 0.051 172.552)"),
        new("Teal 50", "teal-50", "oklch(98.4% 0.014 180.72)"),
        new("Teal 100", "teal-100", "oklch(95.3% 0.051 180.801)"),
        new("Teal 200", "teal-200", "oklch(91% 0.096 180.426)"),
        new("Teal 300", "teal-300", "oklch(85.5% 0.138 181.071)"),
        new("Teal 400", "teal-400", "oklch(77.7% 0.152 181.912)"),
        new("Teal 500", "teal-500", "oklch(70.4% 0.14 182.503)"),
        new("Teal 600", "teal-600", "oklch(60% 0.118 184.704)"),
        new("Teal 700", "teal-700", "oklch(51.1% 0.096 186.391)"),
        new("Teal 800", "teal-800", "oklch(43.7% 0.078 188.216)"),
        new("Teal 900", "teal-900", "oklch(38.6% 0.063 188.416)"),
        new("Teal 950", "teal-950", "oklch(27.7% 0.046 192.524)"),
        new("Cyan 50", "cyan-50", "oklch(98.4% 0.019 200.873)"),
        new("Cyan 100", "cyan-100", "oklch(95.6% 0.045 203.388)"),
        new("Cyan 200", "cyan-200", "oklch(91.7% 0.08 205.041)"),
        new("Cyan 300", "cyan-300", "oklch(86.5% 0.127 207.078)"),
        new("Cyan 400", "cyan-400", "oklch(78.9% 0.154 211.53)"),
        new("Cyan 500", "cyan-500", "oklch(71.5% 0.143 215.221)"),
        new("Cyan 600", "cyan-600", "oklch(60.9% 0.126 221.723)"),
        new("Cyan 700", "cyan-700", "oklch(52% 0.105 223.128)"),
        new("Cyan 800", "cyan-800", "oklch(45% 0.085 224.283)"),
        new("Cyan 900", "cyan-900", "oklch(39.8% 0.07 227.392)"),
        new("Cyan 950", "cyan-950", "oklch(30.2% 0.056 229.695)"),
        new("Sky 50", "sky-50", "oklch(97.7% 0.013 236.62)"),
        new("Sky 100", "sky-100", "oklch(95.1% 0.026 236.824)"),
        new("Sky 200", "sky-200", "oklch(90.1% 0.058 230.902)"),
        new("Sky 300", "sky-300", "oklch(82.8% 0.111 230.318)"),
        new("Sky 400", "sky-400", "oklch(74.6% 0.16 232.661)"),
        new("Sky 500", "sky-500", "oklch(68.5% 0.169 237.323)"),
        new("Sky 600", "sky-600", "oklch(58.8% 0.158 241.966)"),
        new("Sky 700", "sky-700", "oklch(50% 0.134 242.749)"),
        new("Sky 800", "sky-800", "oklch(44.3% 0.11 240.79)"),
        new("Sky 900", "sky-900", "oklch(39.1% 0.09 240.876)"),
        new("Sky 950", "sky-950", "oklch(29.3% 0.066 243.157)"),
        new("Blue 50", "blue-50", "oklch(97% 0.014 254.604)"),
        new("Blue 100", "blue-100", "oklch(93.2% 0.032 255.585)"),
        new("Blue 200", "blue-200", "oklch(88.2% 0.059 254.128)"),
        new("Blue 300", "blue-300", "oklch(80.9% 0.105 251.813)"),
        new("Blue 400", "blue-400", "oklch(70.7% 0.165 254.624)"),
        new("Blue 500", "blue-500", "oklch(62.3% 0.214 259.815)"),
        new("Blue 600", "blue-600", "oklch(54.6% 0.245 262.881)"),
        new("Blue 700", "blue-700", "oklch(48.8% 0.243 264.376)"),
        new("Blue 800", "blue-800", "oklch(42.4% 0.199 265.638)"),
        new("Blue 900", "blue-900", "oklch(37.9% 0.146 265.522)"),
        new("Blue 950", "blue-950", "oklch(28.2% 0.091 267.935)"),
        new("Indigo 50", "indigo-50", "oklch(96.2% 0.018 272.314)"),
        new("Indigo 100", "indigo-100", "oklch(93% 0.034 272.788)"),
        new("Indigo 200", "indigo-200", "oklch(87% 0.065 274.039)"),
        new("Indigo 300", "indigo-300", "oklch(78.5% 0.115 274.713)"),
        new("Indigo 400", "indigo-400", "oklch(67.3% 0.182 276.935)"),
        new("Indigo 500", "indigo-500", "oklch(58.5% 0.233 277.117)"),
        new("Indigo 600", "indigo-600", "oklch(51.1% 0.262 276.966)"),
        new("Indigo 700", "indigo-700", "oklch(45.7% 0.24 277.023)"),
        new("Indigo 800", "indigo-800", "oklch(39.8% 0.195 277.366)"),
        new("Indigo 900", "indigo-900", "oklch(35.9% 0.144 278.697)"),
        new("Indigo 950", "indigo-950", "oklch(25.7% 0.09 281.288)"),
        new("Violet 50", "violet-50", "oklch(96.9% 0.016 293.756)"),
        new("Violet 100", "violet-100", "oklch(94.3% 0.029 294.588)"),
        new("Violet 200", "violet-200", "oklch(89.4% 0.057 293.283)"),
        new("Violet 300", "violet-300", "oklch(81.1% 0.111 293.571)"),
        new("Violet 400", "violet-400", "oklch(70.2% 0.183 293.541)"),
        new("Violet 500", "violet-500", "oklch(60.6% 0.25 292.717)"),
        new("Violet 600", "violet-600", "oklch(54.1% 0.281 293.009)"),
        new("Violet 700", "violet-700", "oklch(49.1% 0.27 292.581)"),
        new("Violet 800", "violet-800", "oklch(43.2% 0.232 292.759)"),
        new("Violet 900", "violet-900", "oklch(38% 0.189 293.745)"),
        new("Violet 950", "violet-950", "oklch(28.3% 0.141 291.089)"),
        new("Purple 50", "purple-50", "oklch(97.7% 0.014 308.299)"),
        new("Purple 100", "purple-100", "oklch(94.6% 0.033 307.174)"),
        new("Purple 200", "purple-200", "oklch(90.2% 0.063 306.703)"),
        new("Purple 300", "purple-300", "oklch(82.7% 0.119 306.383)"),
        new("Purple 400", "purple-400", "oklch(71.4% 0.203 305.504)"),
        new("Purple 500", "purple-500", "oklch(62.7% 0.265 303.9)"),
        new("Purple 600", "purple-600", "oklch(55.8% 0.288 302.321)"),
        new("Purple 700", "purple-700", "oklch(49.6% 0.265 301.924)"),
        new("Purple 800", "purple-800", "oklch(43.8% 0.218 303.724)"),
        new("Purple 900", "purple-900", "oklch(38.1% 0.176 304.987)"),
        new("Purple 950", "purple-950", "oklch(29.1% 0.149 302.717)"),
        new("Fuchsia 50", "fuchsia-50", "oklch(97.7% 0.017 320.058)"),
        new("Fuchsia 100", "fuchsia-100", "oklch(95.2% 0.037 318.852)"),
        new("Fuchsia 200", "fuchsia-200", "oklch(90.3% 0.076 319.62)"),
        new("Fuchsia 300", "fuchsia-300", "oklch(83.3% 0.145 321.434)"),
        new("Fuchsia 400", "fuchsia-400", "oklch(74% 0.238 322.16)"),
        new("Fuchsia 500", "fuchsia-500", "oklch(66.7% 0.295 322.15)"),
        new("Fuchsia 600", "fuchsia-600", "oklch(59.1% 0.293 322.896)"),
        new("Fuchsia 700", "fuchsia-700", "oklch(51.8% 0.253 323.949)"),
        new("Fuchsia 800", "fuchsia-800", "oklch(45.2% 0.211 324.591)"),
        new("Fuchsia 900", "fuchsia-900", "oklch(40.1% 0.17 325.612)"),
        new("Fuchsia 950", "fuchsia-950", "oklch(29.3% 0.136 325.661)"),
        new("Pink 50", "pink-50", "oklch(97.1% 0.014 343.198)"),
        new("Pink 100", "pink-100", "oklch(94.8% 0.028 342.258)"),
        new("Pink 200", "pink-200", "oklch(89.9% 0.061 343.231)"),
        new("Pink 300", "pink-300", "oklch(82.3% 0.12 346.018)"),
        new("Pink 400", "pink-400", "oklch(71.8% 0.202 349.761)"),
        new("Pink 500", "pink-500", "oklch(65.6% 0.241 354.308)"),
        new("Pink 600", "pink-600", "oklch(59.2% 0.249 0.584)"),
        new("Pink 700", "pink-700", "oklch(52.5% 0.223 3.958)"),
        new("Pink 800", "pink-800", "oklch(45.9% 0.187 3.815)"),
        new("Pink 900", "pink-900", "oklch(40.8% 0.153 2.432)"),
        new("Pink 950", "pink-950", "oklch(28.4% 0.109 3.907)"),
        new("Rose 50", "rose-50", "oklch(96.9% 0.015 12.422)"),
        new("Rose 100", "rose-100", "oklch(94.1% 0.03 12.58)"),
        new("Rose 200", "rose-200", "oklch(89.2% 0.058 10.001)"),
        new("Rose 300", "rose-300", "oklch(81% 0.117 11.638)"),
        new("Rose 400", "rose-400", "oklch(71.2% 0.194 13.428)"),
        new("Rose 500", "rose-500", "oklch(64.5% 0.246 16.439)"),
        new("Rose 600", "rose-600", "oklch(58.6% 0.253 17.585)"),
        new("Rose 700", "rose-700", "oklch(51.4% 0.222 16.935)"),
        new("Rose 800", "rose-800", "oklch(45.5% 0.188 13.697)"),
        new("Rose 900", "rose-900", "oklch(41% 0.159 10.272)"),
        new("Rose 950", "rose-950", "oklch(27.1% 0.105 12.094)"),
        new("Slate 50", "slate-50", "oklch(98.4% 0.003 247.858)"),
        new("Slate 100", "slate-100", "oklch(96.8% 0.007 247.896)"),
        new("Slate 200", "slate-200", "oklch(92.9% 0.013 255.508)"),
        new("Slate 300", "slate-300", "oklch(86.9% 0.022 252.894)"),
        new("Slate 400", "slate-400", "oklch(70.4% 0.04 256.788)"),
        new("Slate 500", "slate-500", "oklch(55.4% 0.046 257.417)"),
        new("Slate 600", "slate-600", "oklch(44.6% 0.043 257.281)"),
        new("Slate 700", "slate-700", "oklch(37.2% 0.044 257.287)"),
        new("Slate 800", "slate-800", "oklch(27.9% 0.041 260.031)"),
        new("Slate 900", "slate-900", "oklch(20.8% 0.042 265.755)"),
        new("Slate 950", "slate-950", "oklch(12.9% 0.042 264.695)"),
        new("Gray 50", "gray-50", "oklch(98.5% 0.002 247.839)"),
        new("Gray 100", "gray-100", "oklch(96.7% 0.003 264.542)"),
        new("Gray 200", "gray-200", "oklch(92.8% 0.006 264.531)"),
        new("Gray 300", "gray-300", "oklch(87.2% 0.01 258.338)"),
        new("Gray 400", "gray-400", "oklch(70.7% 0.022 261.325)"),
        new("Gray 500", "gray-500", "oklch(55.1% 0.027 264.364)"),
        new("Gray 600", "gray-600", "oklch(44.6% 0.03 256.802)"),
        new("Gray 700", "gray-700", "oklch(37.3% 0.034 259.733)"),
        new("Gray 800", "gray-800", "oklch(27.8% 0.033 256.848)"),
        new("Gray 900", "gray-900", "oklch(21% 0.034 264.665)"),
        new("Gray 950", "gray-950", "oklch(13% 0.028 261.692)"),
        new("Zinc 50", "zinc-50", "oklch(98.5% 0 0)"),
        new("Zinc 100", "zinc-100", "oklch(96.7% 0.001 286.375)"),
        new("Zinc 200", "zinc-200", "oklch(92% 0.004 286.32)"),
        new("Zinc 300", "zinc-300", "oklch(87.1% 0.006 286.286)"),
        new("Zinc 400", "zinc-400", "oklch(70.5% 0.015 286.067)"),
        new("Zinc 500", "zinc-500", "oklch(55.2% 0.016 285.938)"),
        new("Zinc 600", "zinc-600", "oklch(44.2% 0.017 285.786)"),
        new("Zinc 700", "zinc-700", "oklch(37% 0.013 285.805)"),
        new("Zinc 800", "zinc-800", "oklch(27.4% 0.006 286.033)"),
        new("Zinc 900", "zinc-900", "oklch(21% 0.006 285.885)"),
        new("Zinc 950", "zinc-950", "oklch(14.1% 0.005 285.823)"),
        new("Neutral 50", "neutral-50", "oklch(98.5% 0 0)"),
        new("Neutral 100", "neutral-100", "oklch(97% 0 0)"),
        new("Neutral 200", "neutral-200", "oklch(92.2% 0 0)"),
        new("Neutral 300", "neutral-300", "oklch(87% 0 0)"),
        new("Neutral 400", "neutral-400", "oklch(70.8% 0 0)"),
        new("Neutral 500", "neutral-500", "oklch(55.6% 0 0)"),
        new("Neutral 600", "neutral-600", "oklch(43.9% 0 0)"),
        new("Neutral 700", "neutral-700", "oklch(37.1% 0 0)"),
        new("Neutral 800", "neutral-800", "oklch(26.9% 0 0)"),
        new("Neutral 900", "neutral-900", "oklch(20.5% 0 0)"),
        new("Neutral 950", "neutral-950", "oklch(14.5% 0 0)"),
        new("Stone 50", "stone-50", "oklch(98.5% 0.001 106.423)"),
        new("Stone 100", "stone-100", "oklch(97% 0.001 106.424)"),
        new("Stone 200", "stone-200", "oklch(92.3% 0.003 48.717)"),
        new("Stone 300", "stone-300", "oklch(86.9% 0.005 56.366)"),
        new("Stone 400", "stone-400", "oklch(70.9% 0.01 56.259)"),
        new("Stone 500", "stone-500", "oklch(55.3% 0.013 58.071)"),
        new("Stone 600", "stone-600", "oklch(44.4% 0.011 73.639)"),
        new("Stone 700", "stone-700", "oklch(37.4% 0.01 67.558)"),
        new("Stone 800", "stone-800", "oklch(26.8% 0.007 34.298)"),
        new("Stone 900", "stone-900", "oklch(21.6% 0.006 56.043)"),
        new("Stone 950", "stone-950", "oklch(14.7% 0.004 49.25)"),
        new("Taupe 50", "taupe-50", "oklch(98.6% 0.002 67.8)"),
        new("Taupe 100", "taupe-100", "oklch(96% 0.002 17.2)"),
        new("Taupe 200", "taupe-200", "oklch(92.2% 0.005 34.3)"),
        new("Taupe 300", "taupe-300", "oklch(86.8% 0.007 39.5)"),
        new("Taupe 400", "taupe-400", "oklch(71.4% 0.014 41.2)"),
        new("Taupe 500", "taupe-500", "oklch(54.7% 0.021 43.1)"),
        new("Taupe 600", "taupe-600", "oklch(43.8% 0.017 39.3)"),
        new("Taupe 700", "taupe-700", "oklch(36.7% 0.016 35.7)"),
        new("Taupe 800", "taupe-800", "oklch(26.8% 0.011 36.5)"),
        new("Taupe 900", "taupe-900", "oklch(21.4% 0.009 43.1)"),
        new("Taupe 950", "taupe-950", "oklch(14.7% 0.004 49.3)"),
        new("Mauve 50", "mauve-50", "oklch(98.5% 0 0)"),
        new("Mauve 100", "mauve-100", "oklch(96% 0.003 325.6)"),
        new("Mauve 200", "mauve-200", "oklch(92.2% 0.005 325.62)"),
        new("Mauve 300", "mauve-300", "oklch(86.5% 0.012 325.68)"),
        new("Mauve 400", "mauve-400", "oklch(71.1% 0.019 323.02)"),
        new("Mauve 500", "mauve-500", "oklch(54.2% 0.034 322.5)"),
        new("Mauve 600", "mauve-600", "oklch(43.5% 0.029 321.78)"),
        new("Mauve 700", "mauve-700", "oklch(36.4% 0.029 323.89)"),
        new("Mauve 800", "mauve-800", "oklch(26.3% 0.024 320.12)"),
        new("Mauve 900", "mauve-900", "oklch(21.2% 0.019 322.12)"),
        new("Mauve 950", "mauve-950", "oklch(14.5% 0.008 326)"),
        new("Mist 50", "mist-50", "oklch(98.7% 0.002 197.1)"),
        new("Mist 100", "mist-100", "oklch(96.3% 0.002 197.1)"),
        new("Mist 200", "mist-200", "oklch(92.5% 0.005 214.3)"),
        new("Mist 300", "mist-300", "oklch(87.2% 0.007 219.6)"),
        new("Mist 400", "mist-400", "oklch(72.3% 0.014 214.4)"),
        new("Mist 500", "mist-500", "oklch(56% 0.021 213.5)"),
        new("Mist 600", "mist-600", "oklch(45% 0.017 213.2)"),
        new("Mist 700", "mist-700", "oklch(37.8% 0.015 216)"),
        new("Mist 800", "mist-800", "oklch(27.5% 0.011 216.9)"),
        new("Mist 900", "mist-900", "oklch(21.8% 0.008 223.9)"),
        new("Mist 950", "mist-950", "oklch(14.8% 0.004 228.8)"),
        new("Olive 50", "olive-50", "oklch(98.8% 0.003 106.5)"),
        new("Olive 100", "olive-100", "oklch(96.6% 0.005 106.5)"),
        new("Olive 200", "olive-200", "oklch(93% 0.007 106.5)"),
        new("Olive 300", "olive-300", "oklch(88% 0.011 106.6)"),
        new("Olive 400", "olive-400", "oklch(73.7% 0.021 106.9)"),
        new("Olive 500", "olive-500", "oklch(58% 0.031 107.3)"),
        new("Olive 600", "olive-600", "oklch(46.6% 0.025 107.3)"),
        new("Olive 700", "olive-700", "oklch(39.4% 0.023 107.4)"),
        new("Olive 800", "olive-800", "oklch(28.6% 0.016 107.4)"),
        new("Olive 900", "olive-900", "oklch(22.8% 0.013 107.4)"),
        new("Olive 950", "olive-950", "oklch(15.3% 0.006 107.1)"),
        new("Black", "black", "#000000"),
        new("White", "white", "#FFFFFF")
    ];

    public string Name => "Built-in Base Colours";

    public string Description => "Exposes the built-in color families as selectable primitive swatches.";

    public string Icon => "icon-colorpicker";

    public string Group => "Custom";

    public OverlaySize OverlaySize => OverlaySize.Small;

    public Dictionary<string, object> DefaultValues => new();

    public IEnumerable<ContentmentConfigurationField> Fields => [];

    public IEnumerable<DataListItem> GetItems(Dictionary<string, object> config)
    {
        return Colors.Select(x => new DataListItem
        {
            Name = x.Label,
            Value = x.Alias,
            Description = x.Value
        });
    }

    private sealed record BaseColorDefinition(string Label, string Alias, string Value);

    public static bool TryGetColorValue(string alias, out string value)
    {
        var match = Colors.FirstOrDefault(x => string.Equals(x.Alias, alias, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            value = string.Empty;
            return false;
        }

        value = match.Value;
        return true;
    }
}

public sealed class TailwindPrimitiveColorDataSource : IContentmentDataSource
{
    private readonly BuiltInBaseColorDataSource _inner = new();

    public string Name => _inner.Name;

    public string Description => _inner.Description;

    public string Icon => _inner.Icon;

    public string Group => _inner.Group;

    public OverlaySize OverlaySize => _inner.OverlaySize;

    public Dictionary<string, object> DefaultValues => _inner.DefaultValues;

    public IEnumerable<ContentmentConfigurationField> Fields => _inner.Fields;

    public IEnumerable<DataListItem> GetItems(Dictionary<string, object> config) => _inner.GetItems(config);

    public static bool TryGetColorValue(string alias, out string value) =>
        BuiltInBaseColorDataSource.TryGetColorValue(alias, out value);
}

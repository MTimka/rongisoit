-- Table: public.serv_elron

-- DROP TABLE IF EXISTS public.serv_elron;

CREATE TABLE IF NOT EXISTS public.serv_elron
(
    reis text COLLATE pg_catalog."default",
    liin text COLLATE pg_catalog."default",
    reisi_algus_aeg text COLLATE pg_catalog."default",
    reisi_lopp_aeg text COLLATE pg_catalog."default",
    kiirus integer,
    latitude numeric(12,8),
    longitude numeric(12,8),
    rongi_suund integer,
    erinevus_plaanist text COLLATE pg_catalog."default",
    lisateade text COLLATE pg_catalog."default",
    pohjus_teade text COLLATE pg_catalog."default",
    avalda_kodulehel text COLLATE pg_catalog."default",
    asukoha_uuendus timestamp without time zone NOT NULL,
    reisi_staatus text COLLATE pg_catalog."default",
    viimane_peatus text COLLATE pg_catalog."default",
    CONSTRAINT serv_elron_pkey PRIMARY KEY (asukoha_uuendus)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.serv_elron
    OWNER to postgres;
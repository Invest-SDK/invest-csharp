-- liquibase formatted sql

-- changeset author:1 failOnError:true context:local
-- comment: create t_archive table
create table if not exists t_archive(
    id uuid primary key,
    "date" date not null,
    instrument_id text not null,
    bucket_name text not null,
    file_name text not null,
    constraint unique_date_instrument unique ("date", instrument_id)
);

-- changeset author:2 failOnError:true context:local
-- comment: create t_history_trade table
create table if not exists t_history_trade(
    trade_ts timestamptz not null,
    ticker_cc text not null,
    direction text not null,
    price numeric not null,
    quantity integer not null,
    trade_source text not null,
    instrument_uid uuid,
    archive_id uuid not null,
    constraint fk_archive foreign key(archive_id) references t_archive(id) on delete cascade
);
